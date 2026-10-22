using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VisualInspector.Datos;
using VisualInspector.Seguridad;
using VisualInspector.Servicios.Vision;

namespace VisualInspector.Servicios;

/// <summary>
/// Una fila del listado de autocontroles.
/// </summary>
public sealed record FilaAutocontrol(
    int Id, string CodigoOrden, DateTime FechaControl, int HorasDesdeInicio,
    EstadoAutocontrol Estado, ResultadoAutocontrol? Resultado, string CreadoPor);

/// <summary>
/// Todo lo que se ve en la ficha de un autocontrol, incluido quién y cuándo lo creó o cambió.
/// </summary>
/// <param name="LitografiaOrden">Litografía de la orden (la del primer autocontrol registrado).</param>
/// <param name="Registro">Datos del registro, o null si aún no se ha registrado.</param>
public sealed record FichaAutocontrol(
    int Id, string CodigoOrden, DateTime FechaControl, int HorasDesdeInicio,
    EstadoAutocontrol Estado, string? Observaciones,
    string CreadoPor, DateTime FechaCreacion, string? ModificadoPor, DateTime? FechaModificacion,
    string? LitografiaOrden, DatosRegistro? Registro);

/// <summary>
/// Lo que quedó registrado de un autocontrol: patrón, huellas, tolerancia, resultado y diferencias.
/// </summary>
public sealed record DatosRegistro(
    string? NombreArchivoPatron, string? Litografia, string? VersionPatron,
    string? HuellaPatron, string? HuellaMuestra1, string? HuellaMuestra2,
    string? Tolerancia, decimal? SimilitudMuestra1, decimal? SimilitudMuestra2, decimal? SimilitudEntreMuestras,
    ResultadoAutocontrol? Resultado, bool EsLitografiaDistintaConfirmada,
    string? RegistradoPor, DateTime? FechaRegistro, IReadOnlyList<DiferenciaRegistrada> Diferencias);

/// <summary>
/// Una diferencia registrada, con lo que decidió el operario.
/// </summary>
public sealed record DiferenciaRegistrada(
    int Numero, MotivoDiferencia Motivo, bool EnMuestra1, bool EnMuestra2,
    decimal AnchoMm, decimal AltoMm, decimal AreaMm2,
    ValidacionDiferencia Validacion, string? Comentario, byte[] Recorte, string ValidadoPor, DateTime FechaValidacion);

/// <summary>
/// Una diferencia de la comparación con lo que ha decidido el operario (o null si aún no ha decidido).
/// </summary>
public sealed record DiferenciaValidada(DiferenciaDetectada Diferencia, ValidacionDiferencia? Validacion, string? Comentario);

/// <summary>
/// Todo lo necesario para registrar un autocontrol al terminar la validación.
/// </summary>
public sealed record SolicitudRegistro(
    int AutocontrolId, string NombreArchivoPatron, string Litografia, string VersionPatron,
    string HuellaPatron, string HuellaMuestra1, string HuellaMuestra2,
    NivelTolerancia Tolerancia, double? SimilitudMuestra1, double? SimilitudMuestra2, double? SimilitudEntreMuestras,
    IReadOnlyList<DiferenciaValidada> Diferencias, bool EsLitografiaDistintaConfirmada);

/// <summary>
/// Resultado de crear un autocontrol: su número si ha ido bien, o el error para el usuario.
/// </summary>
public sealed record ResultadoCreacion(int? Id, string? Error);

/// <summary>
/// Gestiona los autocontroles: crearlos, listarlos y ver su ficha.
/// Ver exige el permiso Autocontroles.Ver y crear exige Autocontroles.Realizar,
/// comprobados aquí en el servidor aunque la pantalla ya lo haya hecho.
/// </summary>
public sealed partial class ServicioAutocontroles(
    IDbContextFactory<AppDbContext> fabricaDatos,
    ServicioAutorizacion autorizacion,
    IUsuarioActual usuarioActual)
{
    // Cada cuántas horas se repite el autocontrol durante una orden de fabricación
    public const int HorasEntreControles_wys = 4;
    public const int LongitudMaximaObservaciones_wys = 500;
    private const int MaximoFilasListado_wys = 200;

    // Código de orden de fabricación: exactamente 10 cifras (por ejemplo 1000063961)
    [GeneratedRegex("^[0-9]{10}$")]
    private static partial Regex FormatoOrden();

    /// <summary>
    /// Dice cómo se llama un control según sus horas: "Inicial", "+4 h", "+8 h"…
    /// </summary>
    public static string NombreTipo(int horasDesdeInicio) =>
        horasDesdeInicio == 0 ? "Inicial" : $"+{horasDesdeInicio} h";

    /// <summary>
    /// Explica el estado en palabras sencillas para la pantalla.
    /// </summary>
    public static string NombreEstado(EstadoAutocontrol estado) => estado switch
    {
        EstadoAutocontrol.PendienteArchivos => "Pendiente de archivos",
        EstadoAutocontrol.Registrado => "Registrado",
        _ => estado.ToString()
    };

    /// <summary>
    /// Explica el resultado global en palabras sencillas para la pantalla.
    /// </summary>
    public static string NombreResultado(ResultadoAutocontrol? resultado) => resultado switch
    {
        ResultadoAutocontrol.SinDiferencias => "Sin diferencias",
        ResultadoAutocontrol.DiferenciasCorrectas => "Diferencias correctas",
        ResultadoAutocontrol.ConIncidencias => "Con incidencias",
        _ => "-"
    };

    public const int LongitudMaximaComentario_wys = 500;

    // Versión del patrón: R seguida de cifras (por ejemplo R002)
    [GeneratedRegex("^R[0-9]{1,9}$")]
    private static partial Regex FormatoVersion();

    // Huella SHA-256: 64 caracteres hexadecimales
    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex FormatoHuella();

    /// <summary>
    /// Devuelve los últimos autocontroles, del más reciente al más antiguo.
    /// Si se indica un código de orden (o parte de él), solo los de esas órdenes.
    /// </summary>
    public async Task<List<FilaAutocontrol>> ListarAsync(string? filtroOrden)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesVer_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var consulta = datos.Autocontroles.AsNoTracking();

        // Solo se aceptan cifras en el filtro: cualquier otra cosa se ignora (Zero Trust)
        var filtro = new string((filtroOrden ?? "").Where(char.IsAsciiDigit).ToArray());
        if (filtro.Length > 0)
        {
            consulta = consulta.Where(a => a.OrdenFabricacion.Codigo.StartsWith(filtro));
        }

        return await consulta
            .OrderByDescending(a => a.FechaControl)
            .Take(MaximoFilasListado_wys)
            .Select(a => new FilaAutocontrol(a.Id, a.OrdenFabricacion.Codigo, a.FechaControl,
                a.HorasDesdeInicio, a.Estado, a.Resultado, a.CreadoPor))
            .ToListAsync();
    }

    /// <summary>
    /// Devuelve la ficha de un autocontrol, con su registro y diferencias si ya está registrado,
    /// o null si no existe.
    /// </summary>
    public async Task<FichaAutocontrol?> ObtenerAsync(int id)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesVer_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var a = await datos.Autocontroles.AsNoTracking()
            .Include(x => x.OrdenFabricacion)
            .Include(x => x.Diferencias.OrderBy(d => d.Numero))
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);
        if (a is null)
        {
            return null;
        }

        var registro = a.Estado != EstadoAutocontrol.Registrado ? null : new DatosRegistro(
            a.NombreArchivoPatron, a.Litografia, a.VersionPatron,
            a.HuellaPatron, a.HuellaMuestra1, a.HuellaMuestra2,
            a.Tolerancia, a.SimilitudMuestra1, a.SimilitudMuestra2, a.SimilitudEntreMuestras,
            a.Resultado, a.EsLitografiaDistintaConfirmada, a.RegistradoPor, a.FechaRegistro,
            a.Diferencias.Select(d => new DiferenciaRegistrada(d.Numero, d.Motivo, d.EnMuestra1, d.EnMuestra2,
                d.AnchoMm, d.AltoMm, d.AreaMm2, d.Validacion, d.Comentario, d.Recorte, d.CreadoPor, d.FechaCreacion)).ToList());

        return new FichaAutocontrol(a.Id, a.OrdenFabricacion.Codigo, a.FechaControl,
            a.HorasDesdeInicio, a.Estado, a.Observaciones,
            a.CreadoPor, a.FechaCreacion, a.ModificadoPor, a.FechaModificacion,
            a.OrdenFabricacion.Litografia, registro);
    }

    /// <summary>
    /// Registra un autocontrol al terminar la validación, como quien firma la hoja del control:
    /// guarda el patrón usado, las huellas de los archivos, la tolerancia, el resultado y cada
    /// diferencia con su decisión y su recorte. Todo en una sola operación; una vez registrado
    /// ya no se puede cambiar. Devuelve un mensaje de error para el usuario, o null si va bien.
    /// </summary>
    public async Task<string?> RegistrarAsync(SolicitudRegistro solicitud)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);

        // Zero Trust: todo se comprueba aquí, aunque la pantalla ya lo haya hecho
        var litografia = (solicitud.Litografia ?? "").Trim().ToUpperInvariant();
        var version = (solicitud.VersionPatron ?? "").Trim().ToUpperInvariant();
        if (!ServicioArchivosAutocontrol.EsLitografiaValida(litografia))
            return "La litografía debe ser una L seguida de 7 cifras.";
        if (!FormatoVersion().IsMatch(version))
            return "La versión del patrón debe ser una R seguida de cifras (por ejemplo R002).";
        if (!new[] { solicitud.HuellaPatron, solicitud.HuellaMuestra1, solicitud.HuellaMuestra2 }.All(h => h is not null && FormatoHuella().IsMatch(h)))
            return "Faltan los archivos del autocontrol. Vuelve a cargarlos.";
        if (solicitud.Diferencias.Any(d => d.Validacion is null))
            return "Quedan diferencias sin validar.";
        if (solicitud.Diferencias.Any(d => d.Validacion == ValidacionDiferencia.Incidencia && string.IsNullOrWhiteSpace(d.Comentario)))
            return "Cada incidencia necesita un comentario que explique qué pasa.";
        if (solicitud.Diferencias.Any(d => (d.Comentario?.Trim().Length ?? 0) > LongitudMaximaComentario_wys))
            return $"Los comentarios no pueden pasar de {LongitudMaximaComentario_wys} caracteres.";
        if (solicitud.Diferencias.Select(d => d.Diferencia.Numero).Distinct().Count() != solicitud.Diferencias.Count)
            return "Hay diferencias repetidas. Vuelve a comparar.";

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var autocontrol = await datos.Autocontroles
            .Include(a => a.OrdenFabricacion)
            .FirstOrDefaultAsync(a => a.Id == solicitud.AutocontrolId);
        if (autocontrol is null)
            return "El autocontrol ya no existe.";
        if (autocontrol.Estado == EstadoAutocontrol.Registrado)
            return "Este autocontrol ya está registrado y no se puede cambiar.";

        // La litografía debe ser la de la orden, salvo que el operario confirme que el patrón es correcto
        var litografiaOrden = autocontrol.OrdenFabricacion.Litografia;
        var esDistinta = litografiaOrden is not null && litografiaOrden != litografia;
        if (esDistinta && !solicitud.EsLitografiaDistintaConfirmada)
            return $"La litografía del patrón ({litografia}) no coincide con la de la orden ({litografiaOrden}). Confirma que el patrón es el correcto.";
        autocontrol.OrdenFabricacion.Litografia ??= litografia;

        autocontrol.NombreArchivoPatron = Path.GetFileName(solicitud.NombreArchivoPatron ?? "") is { Length: > 0 } n ? n[..Math.Min(n.Length, 255)] : null;
        autocontrol.Litografia = litografia;
        autocontrol.VersionPatron = version;
        autocontrol.HuellaPatron = solicitud.HuellaPatron;
        autocontrol.HuellaMuestra1 = solicitud.HuellaMuestra1;
        autocontrol.HuellaMuestra2 = solicitud.HuellaMuestra2;
        autocontrol.Tolerancia = solicitud.Tolerancia.ToString();
        autocontrol.SimilitudMuestra1 = Redondear(solicitud.SimilitudMuestra1);
        autocontrol.SimilitudMuestra2 = Redondear(solicitud.SimilitudMuestra2);
        autocontrol.SimilitudEntreMuestras = Redondear(solicitud.SimilitudEntreMuestras);
        autocontrol.EsLitografiaDistintaConfirmada = esDistinta;
        autocontrol.Resultado = solicitud.Diferencias.Count == 0 ? ResultadoAutocontrol.SinDiferencias
            : solicitud.Diferencias.Any(d => d.Validacion == ValidacionDiferencia.Incidencia) ? ResultadoAutocontrol.ConIncidencias
            : ResultadoAutocontrol.DiferenciasCorrectas;
        autocontrol.RegistradoPor = await usuarioActual.ObtenerNombreAsync();
        autocontrol.FechaRegistro = DateTime.Now;
        autocontrol.Estado = EstadoAutocontrol.Registrado;

        foreach (var v in solicitud.Diferencias)
        {
            var d = v.Diferencia;
            autocontrol.Diferencias.Add(new Diferencia
            {
                Numero = d.Numero,
                Motivo = d.Motivo,
                EnMuestra1 = d.EnMuestra1,
                EnMuestra2 = d.EnMuestra2,
                XMm = Math.Round((decimal)d.XMm, 1),
                YMm = Math.Round((decimal)d.YMm, 1),
                AnchoMm = Math.Round((decimal)d.AnchoMm, 1),
                AltoMm = Math.Round((decimal)d.AltoMm, 1),
                AreaMm2 = Math.Round((decimal)d.AreaMm2, 2),
                Evidencia = Math.Round((decimal)d.Evidencia, 2),
                Recorte = d.Recorte,
                Validacion = v.Validacion!.Value,
                Comentario = string.IsNullOrWhiteSpace(v.Comentario) ? null : v.Comentario.Trim()
            });
        }

        // Una sola operación: o se guarda todo, o nada
        await datos.SaveChangesAsync();
        return null;
    }

    private static decimal? Redondear(double? valor) =>
        valor is null ? null : Math.Round((decimal)Math.Clamp(valor.Value, 0, 100), 2);

    /// <summary>
    /// Dice qué tipo de control tocaría ahora para una orden ("Inicial" si es nueva,
    /// "+4 h" si ya tiene el inicial…), para enseñarlo antes de crearlo.
    /// Devuelve null si el código no es válido.
    /// </summary>
    public async Task<string?> PrevisualizarTipoAsync(string? codigoOrden)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);

        var codigo = (codigoOrden ?? "").Trim();
        if (!FormatoOrden().IsMatch(codigo))
        {
            return null;
        }

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var cantidad = await datos.Autocontroles.CountAsync(a => a.OrdenFabricacion.Codigo == codigo);
        return NombreTipo(cantidad * HorasEntreControles_wys);
    }

    /// <summary>
    /// Crea un autocontrol nuevo para una orden de fabricación, como quien abre una hoja nueva
    /// en el cuaderno de la orden. Si la orden aún no existe, la crea. La fecha y hora las pone
    /// el servidor, y el tipo (Inicial, +4 h, +8 h…) se calcula solo.
    /// </summary>
    public async Task<ResultadoCreacion> CrearAsync(string? codigoOrden, string? observaciones)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);

        // Zero Trust: todo se vuelve a validar aquí, aunque la pantalla ya lo haya hecho
        var codigo = (codigoOrden ?? "").Trim();
        if (!FormatoOrden().IsMatch(codigo))
        {
            return new(null, "La orden de fabricación debe tener exactamente 10 cifras, solo números.");
        }

        var textoObservaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        if (textoObservaciones?.Length > LongitudMaximaObservaciones_wys)
        {
            return new(null, $"Las observaciones no pueden pasar de {LongitudMaximaObservaciones_wys} caracteres.");
        }

        // Si dos personas crean a la vez un control de la misma orden, la base de datos rechaza
        // el duplicado (índice único) y se vuelve a intentar con el tipo siguiente
        for (var intento = 1; intento <= 3; intento++)
        {
            await using var datos = await fabricaDatos.CreateDbContextAsync();
            try
            {
                var orden = await datos.OrdenesFabricacion.FirstOrDefaultAsync(o => o.Codigo == codigo);
                if (orden is null)
                {
                    orden = new OrdenFabricacion { Codigo = codigo };
                    datos.OrdenesFabricacion.Add(orden);
                }
                else if (!orden.Activa)
                {
                    return new(null, "Esa orden de fabricación está desactivada.");
                }

                var cantidad = orden.Id == 0 ? 0 : await datos.Autocontroles.CountAsync(a => a.OrdenFabricacionId == orden.Id);
                var autocontrol = new Autocontrol
                {
                    OrdenFabricacion = orden,
                    FechaControl = DateTime.Now,
                    HorasDesdeInicio = cantidad * HorasEntreControles_wys,
                    Estado = EstadoAutocontrol.PendienteArchivos,
                    Observaciones = textoObservaciones
                };
                datos.Autocontroles.Add(autocontrol);
                await datos.SaveChangesAsync();
                return new(autocontrol.Id, null);
            }
            catch (DbUpdateException)
            {
                // Otro autocontrol u orden se ha guardado justo a la vez: se repite con datos frescos
            }
        }

        return new(null, "No se ha podido guardar porque otra persona estaba creando el mismo autocontrol. Inténtalo de nuevo.");
    }
}
