namespace VisualInspector.Servicios;

/// <summary>
/// Fabrica el código que ve el usuario cuando algo falla (por ejemplo ERR-20261021-143012).
/// Es como el número de un ticket: con él, IT encuentra el error en log/err-log.
/// </summary>
public static class CodigoError
{
    /// <summary>
    /// Crea un código con la fecha y la hora actuales (hora de Madrid).
    /// </summary>
    public static string Generar() => $"ERR-{DateTime.Now:yyyyMMdd-HHmmss}";
}
