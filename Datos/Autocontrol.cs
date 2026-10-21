namespace VisualInspector.Datos;

/// <summary>
/// En qué punto está un autocontrol. Irá creciendo con los siguientes pasos del MVP
/// (archivos cargados, comparado, revisado…).
/// </summary>
public enum EstadoAutocontrol
{
    PendienteArchivos
}

/// <summary>
/// Un autocontrol visual de impresión: la comprobación que se hace al inicio de la orden y
/// cada 4 horas, comparando el patrón aprobado con dos tubos de la misma plancha.
/// Forma parte del registro de fabricación, así que nunca se borra.
/// </summary>
public class Autocontrol : EntidadTrazable
{
    public int Id { get; set; }

    public int OrdenFabricacionId { get; set; }
    public OrdenFabricacion OrdenFabricacion { get; set; } = null!;

    // Momento del control, puesto por el servidor (hora de Madrid); no lo escribe nadie a mano
    public DateTime FechaControl { get; set; }

    // 0 = control inicial; 4, 8, 12… = control periódico a esas horas del inicio.
    // Se calcula solo según los autocontroles que ya tenga la orden.
    public int HorasDesdeInicio { get; set; }

    public EstadoAutocontrol Estado { get; set; } = EstadoAutocontrol.PendienteArchivos;

    public string? Observaciones { get; set; }
}
