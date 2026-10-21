namespace VisualInspector.Datos;

/// <summary>
/// Los cuatro datos que dicen quién creó o cambió algo y cuándo, como la firma y la fecha
/// que se ponen al pie de un documento.
/// </summary>
public interface ITrazable
{
    string CreadoPor { get; set; }
    DateTime FechaCreacion { get; set; }
    string? ModificadoPor { get; set; }
    DateTime? FechaModificacion { get; set; }
}

/// <summary>
/// Base para todos los datos que guardan quién y cuándo los creó o modificó.
/// Estos campos se rellenan solos al guardar; no hay que asignarlos a mano.
/// </summary>
public abstract class EntidadTrazable : ITrazable
{
    public string CreadoPor { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
