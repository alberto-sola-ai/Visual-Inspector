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
/// después cuando el operario lo considera (se recomienda cada 4 horas), comparando el patrón
/// aprobado con dos tubos de la misma plancha.
/// Forma parte del registro de fabricación: una vez registrado no se cambia. Solo el administrador
/// puede anularlo (queda en la orden, marcado y con su motivo) o eliminarlo si es erróneo.
/// </summary>
public class Autocontrol : EntidadTrazable
{
    public int Id { get; set; }

    public int OrdenFabricacionId { get; set; }
    public OrdenFabricacion OrdenFabricacion { get; set; } = null!;

    // Momento del control, puesto por el servidor (hora de Madrid); no lo escribe nadie a mano
    public DateTime FechaControl { get; set; }

    // Número del control dentro de la orden: 1 = inicial, 2, 3… Lo hace el operario cuando lo
    // considera (se recomienda cada 4 horas); se calcula solo: el último de la orden más 1.
    public int NumeroControl { get; set; }

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

    // --- Anulación (solo el administrador): el autocontrol sigue en la orden pero no cuenta ---
    public bool EstaAnulado { get; set; }
    public string? MotivoAnulacion { get; set; }
    public string? AnuladoPor { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public List<Diferencia> Diferencias { get; set; } = [];
}
