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
/// Qué se hizo en producción ante una incidencia. No es una escala de gravedad: una incidencia puede
/// provocar varias cosas a la vez (por ejemplo parada de línea y retención de producto), así que se
/// pueden marcar varias. "Sin intervención" va sola. Lo decide el operario (decisión de Alberto, 26/10/2026).
/// </summary>
[Flags]
public enum ActuacionIncidencia
{
    // Aún sin elegir (no vale para registrar una incidencia)
    Ninguna = 0,
    // Se registra y se acepta: no hizo falta tocar la producción
    SinIntervencion = 1,
    // Se corrigió durante la marcha, sin parar
    AjusteEnMarcha = 2,
    // Se detuvo la línea para corregir
    ParadaLinea = 4,
    // Se retuvo o se apartó producto potencialmente afectado
    RetencionProducto = 8,
    // Se avisó a Calidad (solo informativo: no hay que esperar ninguna autorización)
    AvisoCalidad = 16
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

    // Solo en las incidencias: qué se hizo en producción. Null en las correctas y en las incidencias
    // anteriores a la versión 0.15.0 (sin clasificar)
    public ActuacionIncidencia? Actuaciones { get; set; }

    // Impacto aproximado que apunta el operario: minutos de parada de línea (solo con ParadaLinea) y
    // tubos retenidos (solo con RetencionProducto). Son estimaciones y se pueden corregir después
    // (decisión de Alberto, 26/10/2026); quién y cuándo las tocó queda en ModificadoPor
    public int? MinutosParada { get; set; }
    public int? TubosRetenidos { get; set; }
}
