using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VisualInspector.Datos;
using VisualInspector.Seguridad;
using VisualInspector.Servicios.Vision;

namespace VisualInspector.Servicios;

/// <summary>
/// Una fila del listado de órdenes de fabricación.
/// </summary>
public sealed record FilaOrden(
    int Id, string Codigo, string? Litografia, string? Linea, EstadoOrden Estado,
    int Autocontroles, int Registrados, int ConIncidencias, DateTime? PrimerControl, DateTime? UltimoControl);

/// <summary>
/// Un autocontrol dentro de la ficha de su orden.
/// </summary>
/// <param name="Anulacion">Datos de la anulación, o null si no está anulado.</param>
public sealed record AutocontrolDeOrden(
    int Id, DateTime FechaControl, int NumeroControl, EstadoAutocontrol Estado, ResultadoAutocontrol? Resultado,
    string? VersionPatron, int Diferencias, int Incidencias, string? RegistradoPor, DatosAnulacion? Anulacion)
{
    /// <summary>
    /// Dice si el autocontrol cuenta para la orden: los anulados se ven, pero no cuentan.
    /// </summary>
    public bool Cuenta => Anulacion is null;

    /// <summary>
    /// Dice si el autocontrol está registrado y cuenta para la orden.
    /// </summary>
    public bool EsRegistradoValido => Cuenta && Estado == EstadoAutocontrol.Registrado;
}

/// <summary>
/// La ficha de una orden: sus datos y todos sus autocontroles en orden de tiempo.
/// </summary>
public sealed record FichaOrden(
    int Id, string Codigo, string? Litografia, string? Linea, EstadoOrden Estado, DateTime? FechaFin, string? FinalizadaPor,
    string CreadoPor, DateTime FechaCreacion, IReadOnlyList<AutocontrolDeOrden> Autocontroles)
{
    /// <summary>
    /// Cuándo se hizo el primer control de la orden: desde ahí se cuenta el tiempo de los demás.
    /// </summary>
    public DateTime FechaInicio => Autocontroles.Count > 0 ? Autocontroles.Min(a => a.FechaControl) : FechaCreacion;

    /// <summary>
    /// Cuándo se hizo el último control de la orden, o null si aún no tiene ninguno.
    /// </summary>
    public DateTime? FechaUltimoControl => Autocontroles.Count > 0 ? Autocontroles.Max(a => a.FechaControl) : null;
}

/// <summary>
/// Una incidencia en el informe de fabricación, con el autocontrol en el que salió.
/// </summary>
public sealed record IncidenciaInforme(
    int NumeroControl, DateTime FechaControl, int Numero, MotivoDiferencia Motivo, bool EnMuestra1, bool EnMuestra2,
    decimal AnchoMm, decimal AltoMm, string? Comentario, string ValidadoPor, DateTime FechaValidacion, byte[] Recorte,
    ActuacionIncidencia? Actuaciones, int? MinutosParada, int? TubosRetenidos);

/// <summary>
/// Todo el informe de fabricación de una orden: cabecera, resumen, autocontroles e incidencias.
/// </summary>
/// <param name="EsProvisional">La orden aún no está finalizada: el informe puede cambiar.</param>
public sealed record InformeFabricacion(
    FichaOrden Orden, bool EsProvisional, IReadOnlyList<string> VersionesPatron,
    int DiferenciasTotales, int Incidencias, int IncidenciasEnLasDos,
    IReadOnlyDictionary<MotivoDiferencia, int> IncidenciasPorMotivo,
    IReadOnlyList<IncidenciaInforme> DetalleIncidencias, DateTime Generado, string GeneradoPor);

/// <summary>
/// Gestiona las órdenes de fabricación: listarlas, ver su ficha, finalizarlas al terminar la
/// fabricación, reabrirlas y preparar su informe. Ver e informe exigen Autocontroles.Ver; finalizar,
/// Autocontroles.Realizar; reabrir, Administracion (decisiones de Alberto, 22/10/2026).
/// Todo se comprueba en el servidor.
/// </summary>
public sealed partial class ServicioOrdenes(
    IDbContextFactory<AppDbContext> fabricaDatos,
    ServicioAutorizacion autorizacion,
    IUsuarioActual usuarioActual,
    RegistroSeguridad registroSeguridad)
{
    private const int MaximoFilasListado_wys = 200;

    [GeneratedRegex("[^0-9A-Za-z]")]
    private static partial Regex CaracteresNoPermitidos();

    /// <summary>
    /// Dice el estado de la orden en palabras sencillas.
    /// </summary>
    public static string NombreEstado(EstadoOrden estado) =>
        estado == EstadoOrden.Finalizada ? "Finalizada" : "En fabricación";

    /// <summary>
    /// Devuelve las órdenes, de la más reciente a la más antigua. Se puede buscar por el código de
    /// la orden o por la litografía (todo o parte).
    /// </summary>
    public async Task<List<FilaOrden>> ListarAsync(string? filtroOrden, string? filtroLitografia)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesVer_wys);

        // Zero Trust: en los filtros solo se aceptan letras y cifras
        var orden = CaracteresNoPermitidos().Replace(filtroOrden ?? "", "");
        var litografia = CaracteresNoPermitidos().Replace(filtroLitografia ?? "", "").ToUpperInvariant();

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var consulta = datos.OrdenesFabricacion.AsNoTracking();
        // Cualquier parte del número de orden: así basta con las últimas cifras, sin el inicio fijo
        if (orden.Length > 0) consulta = consulta.Where(o => o.Codigo.Contains(orden));
        if (litografia.Length > 0) consulta = consulta.Where(o => o.Litografia != null && o.Litografia.StartsWith(litografia));

        return await consulta
            .OrderByDescending(o => o.FechaCreacion)
            .Take(MaximoFilasListado_wys)
            // Los autocontroles anulados no cuentan en las cifras de la orden
            .Select(o => new FilaOrden(o.Id, o.Codigo, o.Litografia, o.Linea, o.Estado,
                o.Autocontroles.Count(a => !a.EstaAnulado),
                o.Autocontroles.Count(a => !a.EstaAnulado && a.Estado == EstadoAutocontrol.Registrado),
                o.Autocontroles.Count(a => !a.EstaAnulado && a.Resultado == ResultadoAutocontrol.ConIncidencias),
                o.Autocontroles.Min(a => (DateTime?)a.FechaControl),
                o.Autocontroles.Max(a => (DateTime?)a.FechaControl)))
            .ToListAsync();
    }

    /// <summary>
    /// Devuelve la ficha de una orden con todos sus autocontroles, o null si no existe.
    /// </summary>
    public async Task<FichaOrden?> ObtenerAsync(int id)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesVer_wys);
        await using var datos = await fabricaDatos.CreateDbContextAsync();
        return await LeerFichaAsync(datos, id);
    }

    /// <summary>
    /// Finaliza la orden al terminar la fabricación, como quien cierra el cuaderno de la orden:
    /// guarda la fecha y quién la cierra, y a partir de ahí no admite autocontroles nuevos.
    /// No se puede finalizar si no hay ningún autocontrol registrado (los anulados no cuentan).
    /// Sí se puede con autocontroles sin registrar: no se sabe cuántos se harán en una orden. Esos
    /// controles se borran al finalizar, porque ya no se podrían registrar (decisión de Alberto,
    /// 30/10/2026, igual que en el cierre automático), y queda apuntado en el diario de seguridad.
    /// Devuelve un mensaje de error para el usuario, o null si va bien.
    /// </summary>
    public async Task<string?> FinalizarAsync(int id)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var orden = await datos.OrdenesFabricacion
            .Include(o => o.Autocontroles).ThenInclude(a => a.Diferencias)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (orden is null)
            return "La orden ya no existe.";
        if (orden.Estado == EstadoOrden.Finalizada)
            return "La orden ya está finalizada.";

        if (!orden.Autocontroles.Any(a => !a.EstaAnulado && a.Estado == EstadoAutocontrol.Registrado))
            return "La orden no tiene ningún autocontrol registrado.";

        orden.Estado = EstadoOrden.Finalizada;
        orden.FechaFin = DateTime.Now;
        orden.FinalizadaPor = await usuarioActual.ObtenerNombreAsync();
        // Los autocontroles sin registrar se borran en la misma operación
        var borrados = ServicioAutocontroles.BorrarSinRegistrar(datos, orden);
        await datos.SaveChangesAsync();

        foreach (var borrado in borrados)
        {
            await registroSeguridad.RegistrarAsync(AccionesSeguridad.EliminacionAutocontrol_wys,
                $"Correcto: orden {orden.Codigo}, {borrado}, sin registrar, borrado al finalizar la orden");
        }
        return null;
    }

    /// <summary>
    /// Reabre una orden finalizada, como quien vuelve a abrir el cuaderno de la orden, para poder
    /// añadirle más autocontroles. Solo puede hacerlo el administrador.
    /// Devuelve un mensaje de error para el usuario, o null si va bien.
    /// </summary>
    public async Task<string?> ReabrirAsync(int id)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var orden = await datos.OrdenesFabricacion.FirstOrDefaultAsync(o => o.Id == id);
        if (orden is null)
            return "La orden ya no existe.";
        if (orden.Estado != EstadoOrden.Finalizada)
            return "La orden no está finalizada.";

        // Quién y cuándo la reabre queda en la trazabilidad de la orden (ModificadoPor y
        // FechaModificacion) y en el diario de seguridad
        orden.Estado = EstadoOrden.EnFabricacion;
        orden.FechaFin = null;
        orden.FinalizadaPor = null;
        await datos.SaveChangesAsync();
        await registroSeguridad.RegistrarAsync(AccionesSeguridad.ReaperturaOrden_wys, $"Correcto: orden {orden.Codigo}");
        return null;
    }

    /// <summary>
    /// Prepara el informe de fabricación de la orden: todo lo que pasó en sus autocontroles, con el
    /// detalle de cada incidencia y su recorte. Si la orden no está finalizada, es provisional.
    /// Devuelve null si la orden no existe.
    /// </summary>
    public async Task<InformeFabricacion?> ObtenerInformeAsync(int id)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesVer_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var ficha = await LeerFichaAsync(datos, id);
        if (ficha is null)
        {
            return null;
        }

        // Las diferencias de los autocontroles anulados no cuentan en el informe
        var diferencias = await datos.Diferencias.AsNoTracking()
            .Where(d => d.Autocontrol.OrdenFabricacionId == id && !d.Autocontrol.EstaAnulado)
            .Select(d => new
            {
                d.Autocontrol.NumeroControl, d.Autocontrol.FechaControl, d.Numero, d.Motivo, d.EnMuestra1, d.EnMuestra2,
                d.AnchoMm, d.AltoMm, d.Validacion, d.Comentario, d.CreadoPor, d.FechaCreacion, d.Recorte, d.Actuaciones,
                d.MinutosParada, d.TubosRetenidos
            })
            .ToListAsync();

        var incidencias = diferencias
            .Where(d => d.Validacion == ValidacionDiferencia.Incidencia)
            .OrderBy(d => d.FechaControl).ThenBy(d => d.Numero)
            .Select(d => new IncidenciaInforme(d.NumeroControl, d.FechaControl, d.Numero, d.Motivo, d.EnMuestra1, d.EnMuestra2,
                d.AnchoMm, d.AltoMm, d.Comentario, d.CreadoPor, d.FechaCreacion, d.Recorte, d.Actuaciones, d.MinutosParada, d.TubosRetenidos))
            .ToList();

        var versiones = ficha.Autocontroles.Where(a => a.Cuenta).Select(a => a.VersionPatron).Where(v => v is not null).Distinct().Select(v => v!).ToList();
        var porMotivo = Enum.GetValues<MotivoDiferencia>().ToDictionary(m => m, m => incidencias.Count(i => i.Motivo == m));

        return new InformeFabricacion(ficha, ficha.Estado != EstadoOrden.Finalizada, versiones,
            diferencias.Count, incidencias.Count, incidencias.Count(i => i.EnMuestra1 && i.EnMuestra2),
            porMotivo, incidencias, DateTime.Now, await usuarioActual.ObtenerNombreAsync());
    }

    /// <summary>
    /// Lee la ficha de la orden con sus autocontroles, del primero al último.
    /// </summary>
    private static async Task<FichaOrden?> LeerFichaAsync(AppDbContext datos, int id)
    {
        var orden = await datos.OrdenesFabricacion.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (orden is null)
        {
            return null;
        }

        var filas = await datos.Autocontroles.AsNoTracking()
            .Where(a => a.OrdenFabricacionId == id)
            .OrderBy(a => a.FechaControl)
            .Select(a => new
            {
                a.Id, a.FechaControl, a.NumeroControl, a.Estado, a.Resultado, a.VersionPatron,
                Diferencias = a.Diferencias.Count,
                Incidencias = a.Diferencias.Count(d => d.Validacion == ValidacionDiferencia.Incidencia),
                a.RegistradoPor, a.EstaAnulado, a.MotivoAnulacion, a.AnuladoPor, a.FechaAnulacion
            })
            .ToListAsync();
        var autocontroles = filas.Select(a => new AutocontrolDeOrden(a.Id, a.FechaControl, a.NumeroControl, a.Estado, a.Resultado,
                a.VersionPatron, a.Diferencias, a.Incidencias, a.RegistradoPor,
                a.EstaAnulado ? new DatosAnulacion(a.MotivoAnulacion ?? "", a.AnuladoPor ?? "", a.FechaAnulacion ?? a.FechaControl) : null))
            .ToList();

        return new FichaOrden(orden.Id, orden.Codigo, orden.Litografia, orden.Linea, orden.Estado, orden.FechaFin, orden.FinalizadaPor,
            orden.CreadoPor, orden.FechaCreacion, autocontroles);
    }
}
