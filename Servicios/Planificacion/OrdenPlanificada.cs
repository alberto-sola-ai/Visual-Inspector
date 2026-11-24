namespace VisualInspector.Servicios.Planificacion;

/// <summary>
/// Una orden de la hoja de planificación de producción (Google Sheets): en qué línea va, en qué
/// puesto de la cola y cuántos tubos lleva. La de prioridad 1 es la que está cargada en la línea.
/// </summary>
/// <param name="Linea">Línea de la aplicación (L1, L2…), ya traducida desde «LINEA 1»…</param>
/// <param name="Codigo">Orden completa de 10 cifras, con el inicio habitual ya puesto.</param>
/// <param name="Fabricados">Tubos ya fabricados (columna «Fabricados»), o null si no viene.</param>
/// <param name="Pendientes">Tubos que faltan (columna «Pendiente de fabricar»), o null si no viene.</param>
/// <param name="Cantidad">Tubos que pide la orden (columna «CANTIDAD»), o null si no viene.</param>
/// <param name="Especialidad">Código de especialidad (columna «ESPECIALIDAD», por ejemplo E2201359), solo
/// informativo, o null si no viene.</param>
public sealed record OrdenPlanificada(
    string Linea, int Prioridad, string Codigo, string? Descripcion, int? Fabricados, int? Pendientes, int? Cantidad,
    string? Especialidad = null)
{
    /// <summary>
    /// Dice si es la orden que se está fabricando ahora en su línea (prioridad 1). Las demás
    /// están pendientes (decisión de Alberto, 24/11/2026).
    /// </summary>
    public bool EstaEnCurso => Prioridad == 1;
}

/// <summary>
/// Lo último que se sabe de la hoja de planificación: las órdenes leídas y cuándo.
/// </summary>
/// <param name="EstaActiva">La lectura está configurada (clave de Google e identificador de la hoja) y
/// encendida en Parámetros. Si no, la pantalla funciona como antes, solo con la orden escrita a mano.</param>
/// <param name="FechaLectura">Cuándo se leyó bien por última vez, o null si nunca.</param>
/// <param name="UltimaLecturaFallida">El último intento de leer falló: las órdenes son las de antes.</param>
/// <param name="FechaCargaHoja">Cuándo cargó producción los datos en la hoja (se actualiza cada hora),
/// o null si la hoja no lo dice.</param>
public sealed record EstadoPlanificacion(
    bool EstaActiva, IReadOnlyList<OrdenPlanificada> Ordenes, DateTime? FechaLectura, bool UltimaLecturaFallida,
    DateTime? FechaCargaHoja = null)
{
    // Pasado este tiempo sin una lectura buena, los datos se consideran viejos: se avisa y no se
    // propone finalizar órdenes por no estar en la hoja (podría ser un fallo de Google)
    public static readonly TimeSpan AntiguedadMaxima_wys = TimeSpan.FromHours(2);

    public static readonly EstadoPlanificacion Inactiva_wys = new(false, [], null, false);

    /// <summary>
    /// Dice si ya se ha leído la hoja alguna vez, aunque el último intento fallara.
    /// </summary>
    public bool TieneDatos => FechaLectura is not null;

    /// <summary>
    /// Dice si lo leído es de fiar para decidir: la última lectura fue bien y es reciente.
    /// </summary>
    public bool EsReciente(DateTime ahora) =>
        TieneDatos && !UltimaLecturaFallida && ahora - FechaLectura!.Value <= AntiguedadMaxima_wys;
}
