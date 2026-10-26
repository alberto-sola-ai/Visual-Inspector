using Microsoft.EntityFrameworkCore;
using VisualInspector.Datos;
using VisualInspector.Seguridad;
using VisualInspector.Servicios.Vision;

namespace VisualInspector.Servicios;

/// <summary>
/// Una fila de los rankings del cuadro de impacto: el nombre (línea, litografía, defecto u orden) y
/// cuánto costó en producción. Los minutos y los tubos son aproximados: los apunta el operario.
/// </summary>
/// <param name="OrdenId">Solo en el ranking de órdenes: para abrir su trazabilidad.</param>
public sealed record FilaImpacto(
    string Nombre, int? OrdenId, int Incidencias, int ConParada, int Minutos, int ConRetencion, int Tubos);

/// <summary>
/// Todo el cuadro de impacto productivo de un periodo: totales y rankings.
/// </summary>
public sealed record PanelImpacto(
    DateTime Desde, DateTime Hasta, string? Linea,
    int OrdenesConIncidencias, int Incidencias, int SinIntervencion, int ConParada, int Minutos,
    int ConRetencion, int Tubos, int AvisosCalidad, int SinClasificar,
    IReadOnlyList<FilaImpacto> PorLinea, IReadOnlyList<FilaImpacto> PorLitografia,
    IReadOnlyList<FilaImpacto> PorDefecto, IReadOnlyList<FilaImpacto> Ordenes);

/// <summary>
/// Calcula el cuadro de impacto productivo: no cuenta solo cuántas incidencias hubo, sino cuánto
/// costaron (minutos de parada y tubos retenidos), para ver qué líneas, diseños y defectos dan más
/// problemas. Se calcula en el momento con lo ya registrado, así que cada autocontrol que se registra
/// lo actualiza solo. Exige el permiso Autocontroles.Ver, comprobado aquí en el servidor.
/// </summary>
public sealed class ServicioImpacto(
    IDbContextFactory<AppDbContext> fabricaDatos,
    ServicioAutorizacion autorizacion,
    ServicioAutocontroles servicioAutocontroles)
{
    // Cuántas órdenes salen en el ranking de órdenes
    private const int MaximoOrdenes_wys = 15;

    /// <summary>
    /// Prepara el cuadro de impacto del periodo (ambos días incluidos) y, si se indica, de una línea.
    /// Cuenta las incidencias de autocontroles registrados y no anulados, según la fecha del control.
    /// </summary>
    public async Task<PanelImpacto> ObtenerAsync(DateTime desde, DateTime hasta, string? linea)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesVer_wys);

        // Si las fechas vienen al revés se dan la vuelta; el final cuenta hasta el último minuto del día
        if (hasta < desde) (desde, hasta) = (hasta, desde);
        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        // Zero Trust: solo vale una línea de la lista de líneas
        var lineaElegida = (await servicioAutocontroles.ObtenerLineasAsync())
            .FirstOrDefault(l => string.Equals(l, (linea ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var consulta = datos.Diferencias.AsNoTracking()
            .Where(d => d.Validacion == ValidacionDiferencia.Incidencia
                && !d.Autocontrol.EstaAnulado && d.Autocontrol.Estado == EstadoAutocontrol.Registrado
                && d.Autocontrol.FechaControl >= inicio && d.Autocontrol.FechaControl < fin);
        if (lineaElegida is not null)
        {
            consulta = consulta.Where(d => d.Autocontrol.OrdenFabricacion.Linea == lineaElegida);
        }

        // Solo las incidencias, sin los recortes de imagen (pesan mucho y aquí no hacen falta)
        var incidencias = await consulta
            .Select(d => new Incidencia(
                d.Autocontrol.OrdenFabricacionId, d.Autocontrol.OrdenFabricacion.Codigo,
                d.Autocontrol.OrdenFabricacion.Linea, d.Autocontrol.OrdenFabricacion.Litografia,
                d.Motivo, d.Actuaciones, d.MinutosParada, d.TubosRetenidos))
            .ToListAsync();

        return new PanelImpacto(inicio, fin.AddDays(-1), lineaElegida,
            incidencias.Select(i => i.OrdenId).Distinct().Count(), incidencias.Count,
            incidencias.Count(i => Tiene(i, ActuacionIncidencia.SinIntervencion)),
            incidencias.Count(i => Tiene(i, ActuacionIncidencia.ParadaLinea)), incidencias.Sum(i => i.Minutos ?? 0),
            incidencias.Count(i => Tiene(i, ActuacionIncidencia.RetencionProducto)), incidencias.Sum(i => i.Tubos ?? 0),
            incidencias.Count(i => Tiene(i, ActuacionIncidencia.AvisoCalidad)),
            incidencias.Count(i => i.Actuaciones is null or ActuacionIncidencia.Ninguna),
            Agrupar(incidencias, i => i.Linea ?? "Sin línea", null),
            Agrupar(incidencias, i => i.Litografia ?? "Sin litografía", null),
            Agrupar(incidencias, i => NombreDefecto(i.Motivo), null),
            Agrupar(incidencias, i => i.Codigo, i => i.OrdenId).Take(MaximoOrdenes_wys).ToList());
    }

    /// <summary>
    /// Una incidencia con lo justo para las cuentas.
    /// </summary>
    private sealed record Incidencia(
        int OrdenId, string Codigo, string? Linea, string? Litografia, MotivoDiferencia Motivo,
        ActuacionIncidencia? Actuaciones, int? Minutos, int? Tubos);

    /// <summary>
    /// Dice si la incidencia tuvo esa actuación.
    /// </summary>
    private static bool Tiene(Incidencia incidencia, ActuacionIncidencia actuacion) =>
        incidencia.Actuaciones?.HasFlag(actuacion) == true;

    /// <summary>
    /// Nombre del defecto para el ranking, igual que se ve en el resto de pantallas.
    /// </summary>
    private static string NombreDefecto(MotivoDiferencia motivo) => motivo switch
    {
        MotivoDiferencia.Mancha => "Mancha o suciedad",
        MotivoDiferencia.Falta => "Falta elemento o tinta",
        _ => "Desplazamiento o texto doble"
    };

    /// <summary>
    /// Agrupa las incidencias por la clave indicada y suma su coste. El ranking sale ordenado de más a
    /// menos impacto: primero los minutos de parada, después los tubos retenidos y por último el número
    /// de incidencias, para que a igualdad de parada se vea antes la que más tubos retuvo.
    /// </summary>
    private static List<FilaImpacto> Agrupar(
        IEnumerable<Incidencia> incidencias, Func<Incidencia, string> clave, Func<Incidencia, int?>? idOrden) =>
        incidencias
            .GroupBy(clave)
            .Select(g => new FilaImpacto(g.Key, idOrden is null ? null : idOrden(g.First()),
                g.Count(),
                g.Count(i => Tiene(i, ActuacionIncidencia.ParadaLinea)), g.Sum(i => i.Minutos ?? 0),
                g.Count(i => Tiene(i, ActuacionIncidencia.RetencionProducto)), g.Sum(i => i.Tubos ?? 0)))
            .OrderByDescending(f => f.Minutos).ThenByDescending(f => f.Tubos).ThenByDescending(f => f.Incidencias)
            .ThenBy(f => f.Nombre)
            .ToList();
}
