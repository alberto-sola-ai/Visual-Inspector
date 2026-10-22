namespace VisualInspector.Datos;

/// <summary>
/// En qué punto está un autocontrol.
/// </summary>
public enum EstadoAutocontrol
{
    PendienteArchivos,
    Registrado
}

/// <summary>
/// Resultado global de un autocontrol registrado.
/// </summary>
public enum ResultadoAutocontrol
{
    SinDiferencias,
    DiferenciasCorrectas,
    ConIncidencias
}

/// <summary>
/// Un autocontrol visual de impresión: la comprobación que se hace al inicio de la orden y
/// cada 4 horas, comparando el patrón aprobado con dos tubos de la misma plancha.
/// Forma parte del registro de fabricación, así que nunca se borra ni se cambia una vez registrado.
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

    // --- Datos del registro (se rellenan al registrar; los archivos no se guardan) ---

    // Patrón usado: nombre del archivo, litografía y versión
    public string? NombreArchivoPatron { get; set; }
    public string? Litografia { get; set; }
    public string? VersionPatron { get; set; }

    // Huellas SHA-256: demuestran qué archivos exactos se usaron, sin guardarlos
    public string? HuellaPatron { get; set; }
    public string? HuellaMuestra1 { get; set; }
    public string? HuellaMuestra2 { get; set; }

    // Tolerancia usada en la comparación (Estricta, Normal o Amplia)
    public string? Tolerancia { get; set; }

    // Parecido orientativo (%), no es criterio de aceptación
    public decimal? SimilitudMuestra1 { get; set; }
    public decimal? SimilitudMuestra2 { get; set; }
    public decimal? SimilitudEntreMuestras { get; set; }

    public ResultadoAutocontrol? Resultado { get; set; }

    // Si la litografía del patrón no coincidía con la de la orden y el operario confirmó que era correcta
    public bool EsLitografiaDistintaConfirmada { get; set; }

    // Quién y cuándo validó y registró el autocontrol
    public string? RegistradoPor { get; set; }
    public DateTime? FechaRegistro { get; set; }

    public List<Diferencia> Diferencias { get; set; } = [];
}
