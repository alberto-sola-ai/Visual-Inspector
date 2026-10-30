using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VisualInspector.Datos;
using VisualInspector.Seguridad;
using VisualInspector.Servicios.Vision;

namespace VisualInspector.Servicios;

/// <summary>
/// Una fila del listado de órdenes cerradas, con las cifras más importantes de su trazabilidad.
/// </summary>
/// <param name="Eficacia">Porcentaje de correcciones verificadas, o null si la orden no tuvo incidencias.</param>
public sealed record FilaTrazabilidad(
    int Id, string Codigo, string? Litografia, string? Linea, DateTime? FechaFin,
    int ControlesCompletados, int Incidencias, decimal? Eficacia, int MinutosParada, int TubosRetenidos);

/// <summary>
/// Incidencias de un motivo concreto en una orden y cuántas se dieron por corregidas.
/// </summary>
public sealed record IncidenciasPorMotivo(MotivoDiferencia Motivo, int Detectadas, int Verificadas);

/// <summary>
/// Cuántas incidencias de la orden tuvieron cada actuación en producción. Una incidencia puede
/// contar en varias (por ejemplo parada de línea y retención), así que las cifras no suman el total.
/// </summary>
/// <param name="SinClasificar">Incidencias anteriores a que se pidiera qué se hizo.</param>
/// <param name="MinutosParada">Suma de los minutos de parada que apuntó el operario (aproximados).</param>
/// <param name="TubosRetenidos">Suma de los tubos retenidos que apuntó el operario (aproximados).</param>
public sealed record ImpactoProductivo(
    int SinIntervencion, int AjusteEnMarcha, int ParadaLinea, int RetencionProducto, int AvisoCalidad, int SinClasificar,
    int MinutosParada, int TubosRetenidos);

/// <summary>
/// Producción que hay que revisar porque se retuvo producto: lo fabricado desde el último
/// autocontrol sin incidencias hasta el control en el que se retuvo.
/// </summary>
public sealed record TramoARevisar(int ControlDesde, DateTime Desde, int ControlHasta, DateTime Hasta);

/// <summary>
/// Un autocontrol dentro del cuadro de mando de su orden.
/// </summary>
public sealed record ControlDelPanel(
    int Id, int NumeroControl, DateTime FechaControl, EstadoAutocontrol Estado, ResultadoAutocontrol? Resultado,
    int Incidencias, string? VersionPatron);

/// <summary>
/// Las cifras de trazabilidad de una orden: lo que se enseña en su cuadro de mando.
/// </summary>
public sealed record PanelTrazabilidad(
    int Id, string Codigo, string? Litografia, string? Linea, EstadoOrden Estado,
    DateTime Inicio, DateTime? FechaFin, string? FinalizadaPor,
    int ControlesIniciados, int ControlesCompletados,
    int MuestrasEsperadas, int MuestrasEscaneadas,
    int Incidencias, int CorreccionesVerificadas,
    int ControlesIntegros,
    IReadOnlyList<IncidenciasPorMotivo> PorMotivo,
    ImpactoProductivo Impacto,
    IReadOnlyList<TramoARevisar> TramosARevisar,
    IReadOnlyList<ControlDelPanel> Controles,
    IReadOnlyList<string> VersionesPatron)
{
    /// <summary>
    /// Porcentaje de incidencias cuya corrección se verificó, o null si no hubo incidencias.
    /// </summary>
    public decimal? Eficacia => Incidencias == 0 ? null : Math.Round(100m * CorreccionesVerificadas / Incidencias, 1);

    /// <summary>
    /// Porcentaje de autocontroles registrados con todos sus datos de trazabilidad, o null si no hay ninguno.
    /// </summary>
    public decimal? Integridad => ControlesCompletados == 0 ? null : Math.Round(100m * ControlesIntegros / ControlesCompletados, 1);

    /// <summary>
    /// Dice si la orden sigue en fabricación: sus cifras todavía pueden cambiar.
    /// </summary>
    public bool EsProvisional => Estado != EstadoOrden.Finalizada;
}

/// <summary>
/// Calcula la trazabilidad de las órdenes cerradas: cuántos autocontroles se completaron, cuántas
/// incidencias hubo y cuántas se corrigieron de verdad. Todo sale de lo que ya se guarda; no añade
/// datos nuevos. Exige el permiso Autocontroles.Ver, comprobado aquí en el servidor.
/// </summary>
public sealed partial class ServicioTrazabilidad(
    IDbContextFactory<AppDbContext> fabricaDatos,
    ServicioAutorizacion autorizacion)
{
    private const int MaximoFilasListado_wys = 100;

    // Muestras que se escanean en cada autocontrol
    private const int MuestrasPorControl_wys = 2;

    [GeneratedRegex("[^0-9A-Za-z]")]
    private static partial Regex CaracteresNoPermitidos();

    /// <summary>
    /// Lo que hace falta saber de un autocontrol para calcular la trazabilidad de su orden.
    /// </summary>
    private sealed record DatosControl(
        int Id, int NumeroControl, DateTime FechaControl, EstadoAutocontrol Estado, ResultadoAutocontrol? Resultado,
        string? VersionPatron, int Muestras, bool EsIntegro, IReadOnlyList<IncidenciaCalculo> Incidencias)
    {
        public bool EstaRegistrado => Estado == EstadoAutocontrol.Registrado;
    }

    /// <summary>
    /// Una incidencia con lo justo para las cuentas: su motivo y qué se hizo en producción.
    /// </summary>
    private sealed record IncidenciaCalculo(MotivoDiferencia Motivo, ActuacionIncidencia? Actuaciones, int? Minutos, int? Tubos);

    /// <summary>
    /// Enseña las órdenes cerradas, de la que se cerró más tarde a la más antigua. Se puede buscar
    /// por el código de la orden o por la litografía (todo o parte). La pantalla enseña como mucho 100;
    /// la exportación pide un tope mayor con <paramref name="maximo"/>.
    /// </summary>
    public async Task<List<FilaTrazabilidad>> ListarCerradasAsync(string? filtroOrden, string? filtroLitografia, int maximo = MaximoFilasListado_wys)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesVer_wys);

        // Zero Trust: en los filtros solo se aceptan letras y cifras
        var orden = CaracteresNoPermitidos().Replace(filtroOrden ?? "", "");
        var litografia = CaracteresNoPermitidos().Replace(filtroLitografia ?? "", "").ToUpperInvariant();

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var consulta = datos.OrdenesFabricacion.AsNoTracking().Where(o => o.Estado == EstadoOrden.Finalizada);
        if (orden.Length > 0) consulta = consulta.Where(o => o.Codigo.Contains(orden));
        if (litografia.Length > 0) consulta = consulta.Where(o => o.Litografia != null && o.Litografia.StartsWith(litografia));

        var ordenes = await consulta
            .OrderByDescending(o => o.FechaFin)
            .Take(maximo)
            .Select(o => new { o.Id, o.Codigo, o.Litografia, o.Linea, o.FechaFin })
            .ToListAsync();

        // Los autocontroles de todas esas órdenes, de una vez, para no preguntar orden por orden
        var ids = ordenes.Select(o => o.Id).ToList();
        var controles = (await LeerControlesAsync(datos, a => ids.Contains(a.OrdenFabricacionId)))
            .ToLookup(c => c.OrdenId, c => c.Control);

        return ordenes.Select(o =>
        {
            var resumen = Calcular(controles[o.Id].ToList());
            return new FilaTrazabilidad(o.Id, o.Codigo, o.Litografia, o.Linea, o.FechaFin,
                resumen.ControlesCompletados, resumen.Incidencias,
                resumen.Incidencias == 0 ? null : Math.Round(100m * resumen.CorreccionesVerificadas / resumen.Incidencias, 1),
                resumen.Impacto.MinutosParada, resumen.Impacto.TubosRetenidos);
        }).ToList();
    }

    /// <summary>
    /// Prepara el cuadro de mando de una orden. Devuelve null si la orden no existe.
    /// Si la orden aún no está finalizada, el panel es provisional.
    /// </summary>
    public async Task<PanelTrazabilidad?> ObtenerPanelAsync(int id)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesVer_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var orden = await datos.OrdenesFabricacion.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (orden is null)
        {
            return null;
        }

        var controles = (await LeerControlesAsync(datos, a => a.OrdenFabricacionId == id))
            .Select(c => c.Control)
            .OrderBy(c => c.FechaControl).ThenBy(c => c.NumeroControl)
            .ToList();
        var resumen = Calcular(controles);

        var versiones = controles.Where(c => c.EstaRegistrado && c.VersionPatron is not null)
            .Select(c => c.VersionPatron!).Distinct().ToList();

        return new PanelTrazabilidad(orden.Id, orden.Codigo, orden.Litografia, orden.Linea, orden.Estado,
            controles.Count > 0 ? controles[0].FechaControl : orden.FechaCreacion, orden.FechaFin, orden.FinalizadaPor,
            controles.Count, resumen.ControlesCompletados,
            controles.Count * MuestrasPorControl_wys, resumen.MuestrasEscaneadas,
            resumen.Incidencias, resumen.CorreccionesVerificadas, resumen.ControlesIntegros,
            resumen.PorMotivo, resumen.Impacto, resumen.Tramos,
            controles.Select(c => new ControlDelPanel(c.Id, c.NumeroControl, c.FechaControl, c.Estado, c.Resultado,
                c.Incidencias.Count, c.VersionPatron)).ToList(),
            versiones);
    }

    /// <summary>
    /// Lee los autocontroles que cuentan (los anulados no) con lo justo para calcular, sin los
    /// recortes de imagen, que pesan mucho y aquí no hacen falta.
    /// </summary>
    private static async Task<List<(int OrdenId, DatosControl Control)>> LeerControlesAsync(
        AppDbContext datos, System.Linq.Expressions.Expression<Func<Autocontrol, bool>> filtro)
    {
        var filas = await datos.Autocontroles.AsNoTracking()
            .Where(filtro)
            .Where(a => !a.EstaAnulado)
            .Select(a => new
            {
                a.OrdenFabricacionId, a.Id, a.NumeroControl, a.FechaControl, a.Estado, a.Resultado, a.VersionPatron,
                Muestra1 = a.HuellaMuestra1 != null && a.HuellaMuestra1 != "",
                Muestra2 = a.HuellaMuestra2 != null && a.HuellaMuestra2 != "",
                // Todos los datos que piden las normas para poder rastrear el control
                EsIntegro = a.HuellaPatron != null && a.HuellaPatron != ""
                    && a.HuellaMuestra1 != null && a.HuellaMuestra1 != ""
                    && a.HuellaMuestra2 != null && a.HuellaMuestra2 != ""
                    && a.VersionPatron != null && a.VersionPatron != ""
                    && a.Litografia != null && a.Litografia != ""
                    && a.RegistradoPor != null && a.RegistradoPor != ""
                    && a.FechaRegistro != null,
                Incidencias = a.Diferencias.Where(d => d.Validacion == ValidacionDiferencia.Incidencia)
                    .Select(d => new { d.Motivo, d.Actuaciones, d.MinutosParada, d.TubosRetenidos }).ToList()
            })
            .ToListAsync();

        return filas.Select(f => (f.OrdenFabricacionId, new DatosControl(f.Id, f.NumeroControl, f.FechaControl, f.Estado, f.Resultado,
            f.VersionPatron, (f.Muestra1 ? 1 : 0) + (f.Muestra2 ? 1 : 0), f.EsIntegro,
            f.Incidencias.Select(i => new IncidenciaCalculo(i.Motivo, i.Actuaciones, i.MinutosParada, i.TubosRetenidos)).ToList()))).ToList();
    }

    /// <summary>
    /// Las cifras calculadas de una orden.
    /// </summary>
    private sealed record Resumen(
        int ControlesCompletados, int MuestrasEscaneadas, int Incidencias, int CorreccionesVerificadas,
        int ControlesIntegros, IReadOnlyList<IncidenciasPorMotivo> PorMotivo,
        ImpactoProductivo Impacto, IReadOnlyList<TramoARevisar> Tramos);

    /// <summary>
    /// Hace las cuentas de una orden. Una incidencia se da por corregida si en el siguiente
    /// autocontrol registrado ya no se repite ese mismo motivo (decisión de Alberto, 26/10/2026);
    /// las del último control no se pueden comprobar, así que quedan sin verificar.
    /// </summary>
    private static Resumen Calcular(IReadOnlyList<DatosControl> controles)
    {
        var registrados = controles.Where(c => c.EstaRegistrado)
            .OrderBy(c => c.FechaControl).ThenBy(c => c.NumeroControl).ToList();

        var detectadas = new Dictionary<MotivoDiferencia, int>();
        var verificadas = new Dictionary<MotivoDiferencia, int>();
        for (var i = 0; i < registrados.Count; i++)
        {
            var siguiente = i + 1 < registrados.Count ? registrados[i + 1] : null;
            foreach (var motivo in registrados[i].Incidencias.Select(x => x.Motivo))
            {
                detectadas[motivo] = detectadas.GetValueOrDefault(motivo) + 1;

                // Verificada: existe un control siguiente y en él ese motivo ya no aparece
                if (siguiente is not null && !siguiente.Incidencias.Any(x => x.Motivo == motivo))
                {
                    verificadas[motivo] = verificadas.GetValueOrDefault(motivo) + 1;
                }
            }
        }

        var porMotivo = detectadas
            .Select(d => new IncidenciasPorMotivo(d.Key, d.Value, verificadas.GetValueOrDefault(d.Key)))
            .OrderByDescending(m => m.Detectadas).ThenBy(m => m.Motivo)
            .ToList();

        return new Resumen(registrados.Count, registrados.Sum(c => c.Muestras),
            detectadas.Values.Sum(), verificadas.Values.Sum(),
            registrados.Count(c => c.EsIntegro), porMotivo,
            CalcularImpacto(registrados), CalcularTramos(registrados));
    }

    /// <summary>
    /// Cuenta cuántas incidencias tuvieron cada actuación. Una incidencia puede contar en varias.
    /// </summary>
    private static ImpactoProductivo CalcularImpacto(IReadOnlyList<DatosControl> registrados)
    {
        var incidencias = registrados.SelectMany(c => c.Incidencias).ToList();
        int Contar(ActuacionIncidencia actuacion) => incidencias.Count(i => i.Actuaciones?.HasFlag(actuacion) == true);

        return new ImpactoProductivo(
            Contar(ActuacionIncidencia.SinIntervencion), Contar(ActuacionIncidencia.AjusteEnMarcha),
            Contar(ActuacionIncidencia.ParadaLinea), Contar(ActuacionIncidencia.RetencionProducto),
            Contar(ActuacionIncidencia.AvisoCalidad),
            incidencias.Count(i => i.Actuaciones is null or ActuacionIncidencia.Ninguna),
            incidencias.Sum(i => i.Minutos ?? 0), incidencias.Sum(i => i.Tubos ?? 0));
    }

    /// <summary>
    /// Saca la producción a revisar: por cada control con retención de producto, desde el último
    /// autocontrol sin incidencias anterior (o el primero de la orden, si no lo hubo) hasta ese control.
    /// Si varios controles seguidos retienen producto sin que haya uno limpio en medio, forman un solo tramo.
    /// </summary>
    private static List<TramoARevisar> CalcularTramos(IReadOnlyList<DatosControl> registrados)
    {
        var tramos = new List<TramoARevisar>();
        var inicioAnterior = -1;
        for (var i = 0; i < registrados.Count; i++)
        {
            if (!registrados[i].Incidencias.Any(x => x.Actuaciones?.HasFlag(ActuacionIncidencia.RetencionProducto) == true))
            {
                continue;
            }

            // Hacia atrás hasta el último control limpio; si no hay, desde el primer control de la orden
            var inicio = i - 1;
            while (inicio > 0 && registrados[inicio].Incidencias.Count > 0)
            {
                inicio--;
            }
            inicio = Math.Max(inicio, 0);

            var desde = registrados[inicio];
            var hasta = registrados[i];
            if (inicio == inicioAnterior && tramos.Count > 0)
            {
                // Mismo punto de partida que el tramo anterior: se alarga en lugar de repetirlo
                tramos[^1] = tramos[^1] with { ControlHasta = hasta.NumeroControl, Hasta = hasta.FechaControl };
            }
            else
            {
                tramos.Add(new TramoARevisar(desde.NumeroControl, desde.FechaControl, hasta.NumeroControl, hasta.FechaControl));
            }
            inicioAnterior = inicio;
        }

        return tramos;
    }
}
