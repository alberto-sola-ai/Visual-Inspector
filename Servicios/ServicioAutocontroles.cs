using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VisualInspector.Datos;
using VisualInspector.Seguridad;
using VisualInspector.Servicios.Vision;

namespace VisualInspector.Servicios;

/// <summary>
/// Una fila del listado de autocontroles.
/// </summary>
/// <param name="FechaInicioOrden">Fecha del primer control de la orden, para calcular el tiempo transcurrido.</param>
public sealed record FilaAutocontrol(
    int Id, string CodigoOrden, DateTime FechaControl, int NumeroControl, DateTime FechaInicioOrden,
    EstadoAutocontrol Estado, ResultadoAutocontrol? Resultado, string CreadoPor, bool EstaAnulado, string? Linea);

/// <summary>
/// Por qué, quién y cuándo se anuló un autocontrol.
/// </summary>
public sealed record DatosAnulacion(string Motivo, string AnuladoPor, DateTime Fecha);

/// <summary>
/// Todo lo que se ve en la ficha de un autocontrol, incluido quién y cuándo lo creó o cambió.
/// </summary>
/// <param name="LitografiaOrden">Litografía de la orden (la del primer autocontrol registrado).</param>
/// <param name="Registro">Datos del registro, o null si aún no se ha registrado.</param>
/// <param name="Anulacion">Datos de la anulación, o null si no está anulado.</param>
/// <param name="FechaInicioOrden">Fecha del primer control de la orden, para calcular el tiempo transcurrido.</param>
public sealed record FichaAutocontrol(
    int Id, int OrdenId, string CodigoOrden, EstadoOrden EstadoOrden, DateTime FechaControl, int NumeroControl, DateTime FechaInicioOrden,
    EstadoAutocontrol Estado, string? Observaciones,
    string CreadoPor, DateTime FechaCreacion, string? ModificadoPor, DateTime? FechaModificacion,
    string? LitografiaOrden, DatosRegistro? Registro, DatosAnulacion? Anulacion, string? LineaOrden);

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
/// Lo que se enseña antes de crear un autocontrol: qué control será y cuándo se hizo el último.
/// </summary>
/// <param name="FechaUltimo">Fecha del último control de la orden, o null si será el inicial.</param>
/// <param name="LineaOrden">Línea que tiene la orden (la de su último autocontrol), o null si es nueva.</param>
public sealed record PrevisionControl(string Nombre, DateTime? FechaUltimo, string? LineaOrden);

/// <summary>
/// Una orden que sigue en fabricación, para sugerirla al crear un autocontrol nuevo y no tener
/// que escribir otra vez la orden ni elegir la línea.
/// </summary>
/// <param name="SiguienteControl">El control que tocaría ahora ("Control 3"…).</param>
public sealed record OrdenAbierta(string Codigo, string? Linea, string? Litografia, DateTime UltimoControl, string SiguienteControl);

/// <summary>
/// Resultado de crear un autocontrol: su número si ha ido bien, o el error para el usuario.
/// </summary>
public sealed record ResultadoCreacion(int? Id, string? Error);

/// <summary>
/// Gestiona los autocontroles: crearlos, listarlos, ver su ficha, registrarlos, y anularlos o
/// eliminarlos. Ver exige el permiso Autocontroles.Ver, crear y registrar Autocontroles.Realizar,
/// y anular o eliminar Administracion, comprobados aquí en el servidor aunque la pantalla ya lo
/// haya hecho.
/// </summary>
public sealed partial class ServicioAutocontroles(
    IDbContextFactory<AppDbContext> fabricaDatos,
    ServicioAutorizacion autorizacion,
    IUsuarioActual usuarioActual,
    RegistroSeguridad registroSeguridad,
    ServicioParametros parametros)
{
    // Cada cuántas horas se recomienda hacer un autocontrol. Es solo un aviso: el operario
    // decide cuándo hacerlo (decisión de Alberto, 22/10/2026)
    public const int HorasRecomendadasEntreControles_wys = 4;
    public const int LongitudMaximaObservaciones_wys = 500;
    private const int MaximoFilasListado_wys = 200;

    // Código de orden de fabricación: exactamente 10 cifras (por ejemplo 1000063961)
    public const int CifrasOrden_wys = 10;
    [GeneratedRegex("^[0-9]{10}$")]
    private static partial Regex FormatoOrden();

    /// <summary>
    /// El inicio habitual de las órdenes (parámetro PrefijoOrden, por ejemplo "10000"), con el que
    /// viene relleno el campo de la orden al crear un autocontrol. Si el parámetro no es válido,
    /// se toma vacío y el campo sale en blanco.
    /// </summary>
    public async Task<string> ObtenerPrefijoOrdenAsync()
    {
        var prefijo = (await parametros.ObtenerValorAsync(ServicioParametros.PrefijoOrden_wys, ServicioParametros.PrefijoOrdenInicial_wys)).Trim();
        return prefijo.Length < CifrasOrden_wys && prefijo.All(char.IsAsciiDigit) ? prefijo : "";
    }

    /// <summary>
    /// Dice cómo se llama un control según su número: "Control 1 (inicial)", "Control 2"…
    /// </summary>
    public static string NombreControl(int numeroControl) =>
        numeroControl == 1 ? "Control 1 (inicial)" : $"Control {numeroControl}";

    /// <summary>
    /// Nombre del control con el tiempo que pasó de verdad desde el primero de la orden, por
    /// ejemplo "Control 2 · +3 h 50 min". El primero se queda solo con su nombre.
    /// </summary>
    public static string NombreControlConTiempo(int numeroControl, DateTime fechaControl, DateTime fechaInicioOrden) =>
        fechaControl > fechaInicioOrden
            ? $"{NombreControl(numeroControl)} · {TiempoDesdeInicio(fechaControl, fechaInicioOrden)}"
            : NombreControl(numeroControl);

    /// <summary>
    /// Tiempo que pasó desde el primer control de la orden, por ejemplo "+3 h 50 min".
    /// Vacío para el primero.
    /// </summary>
    public static string TiempoDesdeInicio(DateTime fechaControl, DateTime fechaInicioOrden) =>
        fechaControl > fechaInicioOrden ? $"+{FormatearDuracion(fechaControl - fechaInicioOrden)}" : "";

    /// <summary>
    /// Escribe un tiempo de forma sencilla: "45 min", "3 h 50 min", "26 h".
    /// </summary>
    public static string FormatearDuracion(TimeSpan duracion)
    {
        var horas = (int)duracion.TotalHours;
        var minutos = duracion.Minutes;
        if (horas == 0) return $"{minutos} min";
        return minutos == 0 ? $"{horas} h" : $"{horas} h {minutos} min";
    }

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
    public const int LongitudMaximaMotivoAnulacion_wys = 500;

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
            // Cualquier parte del número: así basta con las últimas cifras, sin el inicio fijo
            consulta = consulta.Where(a => a.OrdenFabricacion.Codigo.Contains(filtro));
        }

        return await consulta
            .OrderByDescending(a => a.FechaControl)
            .Take(MaximoFilasListado_wys)
            .Select(a => new FilaAutocontrol(a.Id, a.OrdenFabricacion.Codigo, a.FechaControl,
                a.NumeroControl, a.OrdenFabricacion.Autocontroles.Min(x => x.FechaControl),
                a.Estado, a.Resultado, a.CreadoPor, a.EstaAnulado, a.OrdenFabricacion.Linea))
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

        var anulacion = !a.EstaAnulado ? null
            : new DatosAnulacion(a.MotivoAnulacion ?? "", a.AnuladoPor ?? "", a.FechaAnulacion ?? a.FechaModificacion ?? a.FechaCreacion);
        var fechaInicioOrden = await datos.Autocontroles.Where(x => x.OrdenFabricacionId == a.OrdenFabricacionId).MinAsync(x => x.FechaControl);

        return new FichaAutocontrol(a.Id, a.OrdenFabricacionId, a.OrdenFabricacion.Codigo, a.OrdenFabricacion.Estado, a.FechaControl,
            a.NumeroControl, fechaInicioOrden, a.Estado, a.Observaciones,
            a.CreadoPor, a.FechaCreacion, a.ModificadoPor, a.FechaModificacion,
            a.OrdenFabricacion.Litografia, registro, anulacion, a.OrdenFabricacion.Linea);
    }

    /// <summary>
    /// Anula un autocontrol, como quien tacha una hoja del cuaderno sin arrancarla: sigue en la
    /// orden con su motivo, pero ya no cuenta en el informe. Solo el administrador. Se puede hacer
    /// aunque la orden esté finalizada, sin reabrirla: su informe se actualiza solo
    /// (decisión de Alberto, 22/10/2026). Devuelve un mensaje de error para el usuario, o null si va bien.
    /// </summary>
    public async Task<string?> AnularAsync(int id, string? motivo)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        // Zero Trust: el motivo se vuelve a comprobar aquí
        var textoMotivo = (motivo ?? "").Trim();
        if (textoMotivo.Length == 0)
            return "Escribe el motivo de la anulación.";
        if (textoMotivo.Length > LongitudMaximaMotivoAnulacion_wys)
            return $"El motivo no puede pasar de {LongitudMaximaMotivoAnulacion_wys} caracteres.";

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var autocontrol = await datos.Autocontroles.Include(a => a.OrdenFabricacion).FirstOrDefaultAsync(a => a.Id == id);
        if (autocontrol is null)
            return "El autocontrol ya no existe.";
        if (autocontrol.EstaAnulado)
            return "Este autocontrol ya está anulado.";

        autocontrol.EstaAnulado = true;
        autocontrol.MotivoAnulacion = textoMotivo;
        autocontrol.AnuladoPor = await usuarioActual.ObtenerNombreAsync();
        autocontrol.FechaAnulacion = DateTime.Now;
        await datos.SaveChangesAsync();

        // En el diario no se copia el motivo: puede ser largo y ya queda en la base de datos
        await registroSeguridad.RegistrarAsync(AccionesSeguridad.AnulacionAutocontrol_wys,
            $"Correcto: orden {autocontrol.OrdenFabricacion.Codigo}, {NombreControl(autocontrol.NumeroControl)} del {autocontrol.FechaControl:dd/MM/yyyy HH:mm}");
        return null;
    }

    /// <summary>
    /// Elimina un autocontrol erróneo con todas sus diferencias, como quien arranca una hoja mal
    /// hecha: desaparece de la orden y del informe. Solo el administrador; se puede hacer aunque la
    /// orden esté finalizada, sin reabrirla, y su informe se actualiza solo. Queda apuntado en el
    /// diario de seguridad (decisiones de Alberto, 22/10/2026).
    /// Devuelve un mensaje de error para el usuario, o null si va bien.
    /// </summary>
    public async Task<string?> EliminarAsync(int id)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var autocontrol = await datos.Autocontroles
            .Include(a => a.OrdenFabricacion)
            .Include(a => a.Diferencias)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (autocontrol is null)
            return "El autocontrol ya no existe.";

        // Lo que se apunta en el diario se prepara antes de borrar
        var descripcion = $"orden {autocontrol.OrdenFabricacion.Codigo} ({NombreEstadoOrden(autocontrol.OrdenFabricacion.Estado)}), " +
            $"{NombreControl(autocontrol.NumeroControl)} " +
            $"del {autocontrol.FechaControl:dd/MM/yyyy HH:mm}, " +
            $"{(autocontrol.EstaAnulado ? "anulado" : autocontrol.Estado == EstadoAutocontrol.Registrado ? NombreResultado(autocontrol.Resultado).ToLowerInvariant() : "sin registrar")}, " +
            $"{autocontrol.Diferencias.Count} diferencia(s)";

        // Una sola operación: se borran las diferencias (con sus recortes) y el autocontrol
        datos.Diferencias.RemoveRange(autocontrol.Diferencias);
        datos.Autocontroles.Remove(autocontrol);
        await datos.SaveChangesAsync();

        await registroSeguridad.RegistrarAsync(AccionesSeguridad.EliminacionAutocontrol_wys, $"Correcto: {descripcion}");
        return null;
    }

    private static string NombreEstadoOrden(EstadoOrden estado) =>
        estado == EstadoOrden.Finalizada ? "finalizada" : "en fabricación";

    /// <summary>
    /// Calcula el número del siguiente control de una orden: el último que tenga más 1, o 1
    /// (inicial) si no tiene ninguno. Así, aunque se haya eliminado uno del medio, el siguiente
    /// nunca coincide con uno que ya existe.
    /// </summary>
    private static async Task<int> CalcularNumeroSiguienteAsync(AppDbContext datos, int idOrden)
    {
        var ultimo = await datos.Autocontroles
            .Where(a => a.OrdenFabricacionId == idOrden)
            .MaxAsync(a => (int?)a.NumeroControl);
        return (ultimo ?? 0) + 1;
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
        if (autocontrol.EstaAnulado)
            return "Este autocontrol está anulado y ya no se puede registrar.";

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
    /// Dice qué control tocaría ahora para una orden ("Control 1 (inicial)" si es nueva,
    /// "Control 2" si ya tiene el inicial…) y cuándo se hizo el último, para enseñarlo antes de
    /// crearlo. Devuelve null si el código no es válido.
    /// </summary>
    public async Task<PrevisionControl?> PrevisualizarControlAsync(string? codigoOrden)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);

        var codigo = (codigoOrden ?? "").Trim();
        if (!FormatoOrden().IsMatch(codigo))
        {
            return null;
        }

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var orden = await datos.OrdenesFabricacion.Where(o => o.Codigo == codigo)
            .Select(o => new { o.Id, o.Linea }).FirstOrDefaultAsync();
        if (orden is null)
        {
            return new PrevisionControl(NombreControl(1), null, null);
        }

        var ultimo = await datos.Autocontroles.Where(a => a.OrdenFabricacionId == orden.Id).MaxAsync(a => (DateTime?)a.FechaControl);
        return new PrevisionControl(NombreControl(await CalcularNumeroSiguienteAsync(datos, orden.Id)), ultimo, orden.Linea);
    }

    // Cuántas órdenes abiertas se sugieren como mucho al crear un autocontrol
    public const int MaximoOrdenesAbiertas_wys = 10;

    /// <summary>
    /// Las órdenes que siguen en fabricación (activas, sin finalizar y con algún autocontrol), con
    /// la más reciente primero, para sugerirlas al crear un autocontrol nuevo. Como mucho 10.
    /// </summary>
    public async Task<IReadOnlyList<OrdenAbierta>> ListarOrdenesAbiertasAsync()
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var ordenes = await datos.OrdenesFabricacion.AsNoTracking()
            .Where(o => o.Activa && o.Estado == EstadoOrden.EnFabricacion && o.Autocontroles.Any())
            .Select(o => new
            {
                o.Codigo, o.Linea, o.Litografia,
                Ultimo = o.Autocontroles.Max(a => a.FechaControl),
                UltimoNumero = o.Autocontroles.Max(a => a.NumeroControl)
            })
            .OrderByDescending(o => o.Ultimo)
            .Take(MaximoOrdenesAbiertas_wys)
            .ToListAsync();

        return ordenes.Select(o => new OrdenAbierta(o.Codigo, o.Linea, o.Litografia, o.Ultimo, NombreControl(o.UltimoNumero + 1))).ToList();
    }

    /// <summary>
    /// Las líneas de fabricación que se pueden elegir (parámetro Lineas: L1, L2…).
    /// </summary>
    public async Task<IReadOnlyList<string>> ObtenerLineasAsync() =>
        ServicioParametros.SepararLineas(await parametros.ObtenerValorAsync(ServicioParametros.Lineas_wys, ServicioParametros.LineasIniciales_wys));

    /// <summary>
    /// Crea un autocontrol nuevo para una orden de fabricación, como quien abre una hoja nueva
    /// en el cuaderno de la orden. Si la orden aún no existe, la crea. La fecha y hora las pone
    /// el servidor, y el número (1 = inicial, 2, 3…) se calcula solo. La línea elegida (obligatoria)
    /// queda como línea de la orden.
    /// </summary>
    public async Task<ResultadoCreacion> CrearAsync(string? codigoOrden, string? linea, string? observaciones)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);

        // Zero Trust: todo se vuelve a validar aquí, aunque la pantalla ya lo haya hecho
        var codigo = (codigoOrden ?? "").Trim();
        if (!FormatoOrden().IsMatch(codigo))
        {
            return new(null, "La orden de fabricación debe tener exactamente 10 cifras, solo números.");
        }

        // La línea es obligatoria y solo vale una de la lista del parámetro Lineas
        var lineaElegida = (await ObtenerLineasAsync())
            .FirstOrDefault(l => string.Equals(l, (linea ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
        if (lineaElegida is null)
        {
            return new(null, "Elige la línea de fabricación.");
        }

        var textoObservaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        if (textoObservaciones?.Length > LongitudMaximaObservaciones_wys)
        {
            return new(null, $"Las observaciones no pueden pasar de {LongitudMaximaObservaciones_wys} caracteres.");
        }

        // Si dos personas crean a la vez un control de la misma orden, la base de datos rechaza
        // el duplicado (índice único) y se vuelve a intentar con el número siguiente
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
                else if (orden.Estado == EstadoOrden.Finalizada)
                {
                    return new(null, "Esa orden de fabricación está finalizada. Para añadirle autocontroles, pide al administrador que la reabra.");
                }

                // La orden se queda con la línea de su último autocontrol
                orden.Linea = lineaElegida;

                var numero = orden.Id == 0 ? 1 : await CalcularNumeroSiguienteAsync(datos, orden.Id);
                var autocontrol = new Autocontrol
                {
                    OrdenFabricacion = orden,
                    FechaControl = DateTime.Now,
                    NumeroControl = numero,
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
