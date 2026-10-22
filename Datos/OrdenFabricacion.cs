namespace VisualInspector.Datos;

/// <summary>
/// Una orden de fabricación (por ejemplo 1000063961). Agrupa todos los autocontroles que se
/// hacen mientras se fabrica: el inicial y los de cada 4 horas.
/// </summary>
public class OrdenFabricacion : EntidadTrazable
{
    public int Id { get; set; }

    // Código de la orden: exactamente 10 cifras
    public string Codigo { get; set; } = "";

    public string? Descripcion { get; set; }

    // Litografía del diseño (L + 7 cifras): la del primer autocontrol registrado de la orden.
    // Si un autocontrol posterior usa otra, se avisa (posible patrón equivocado).
    public string? Litografia { get; set; }

    // Las órdenes no se borran (perderíamos el rastro de sus autocontroles): se desactivan
    public bool Activa { get; set; } = true;

    public List<Autocontrol> Autocontroles { get; set; } = [];
}
