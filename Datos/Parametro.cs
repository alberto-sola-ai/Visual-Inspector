namespace VisualInspector.Datos;

/// <summary>
/// Qué clase de valor guarda un parámetro, para mostrar el campo adecuado al editarlo.
/// </summary>
public enum TipoParametro
{
    Texto,
    Numero,
    SiNo,
    Fecha,
    Lista
}

/// <summary>
/// Un ajuste de la aplicación que el administrador puede cambiar sin tocar el código,
/// como el interruptor de una luz que se enciende o apaga desde la pared.
/// </summary>
public class Parametro : EntidadTrazable
{
    public int Id { get; set; }

    // Nombre interno, sin espacios (por ejemplo "MfaActivo")
    public string Clave { get; set; } = "";

    // Valor actual guardado como texto; los Sí/No se guardan como "Si" o "No"
    public string Valor { get; set; } = "";

    // Explicación sencilla de para qué sirve, que se ve en la pantalla de parámetros
    public string Descripcion { get; set; } = "";

    public TipoParametro Tipo { get; set; } = TipoParametro.Texto;

    // Solo para el tipo Lista: los valores permitidos separados por "|" (por ejemplo "Estricta|Normal|Amplia")
    public string? Opciones { get; set; }

    // Algunos parámetros no se pueden cambiar todavía (por ejemplo, los de IA mientras la
    // aplicación no use IA); EsEditable = false los muestra bloqueados con su motivo
    public bool EsEditable { get; set; } = true;
    public string? MotivoNoEditable { get; set; }
}
