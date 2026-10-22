using VisualInspector.Servicios.Vision;

namespace VisualInspector.Datos;

/// <summary>
/// Lo que decide el operario sobre una diferencia.
/// </summary>
public enum ValidacionDiferencia
{
    Correcta,
    Incidencia
}

/// <summary>
/// Una diferencia encontrada en un autocontrol y lo que decidió el operario sobre ella. Guarda un
/// recorte pequeño en color como evidencia para el informe de fabricación (los archivos completos
/// no se guardan).
/// </summary>
public class Diferencia : EntidadTrazable
{
    public int Id { get; set; }

    public int AutocontrolId { get; set; }
    public Autocontrol Autocontrol { get; set; } = null!;

    // Número con el que aparecía en la vista general
    public int Numero { get; set; }

    public MotivoDiferencia Motivo { get; set; }

    // En qué muestra(s) aparece: en las dos puede ser un fallo de plancha
    public bool EnMuestra1 { get; set; }
    public bool EnMuestra2 { get; set; }

    // Zona sobre el patrón, en mm desde la esquina superior izquierda de la página
    public decimal XMm { get; set; }
    public decimal YMm { get; set; }
    public decimal AnchoMm { get; set; }
    public decimal AltoMm { get; set; }

    // Superficie realmente distinta y puntuación de evidencia
    public decimal AreaMm2 { get; set; }
    public decimal Evidencia { get; set; }

    // Recorte JPEG en color: patrón a la izquierda y muestra(s) a la derecha
    public byte[] Recorte { get; set; } = [];

    public ValidacionDiferencia Validacion { get; set; }

    // Obligatorio en las incidencias
    public string? Comentario { get; set; }
}
