namespace VisualInspector.Datos;

/// <summary>
/// En qué punto está una orden de fabricación.
/// </summary>
public enum EstadoOrden
{
    EnFabricacion,
    Finalizada
}

/// <summary>
/// Una orden de fabricación (por ejemplo 1000063961). Agrupa todos los autocontroles que se
/// hacen mientras se fabrica: el inicial y los que el operario considere (se recomienda cada 4 horas).
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

    // Línea de fabricación (L1, L2…): se elige en cada autocontrol y la orden se queda con la del
    // último. Sirve para los futuros KPI de eficiencia por línea. Null = órdenes anteriores a 0.12.0
    public string? Linea { get; set; }

    // Las órdenes no se borran (perderíamos el rastro de sus autocontroles): se desactivan
    public bool Activa { get; set; } = true;

    // Al terminar la fabricación se finaliza: ya no admite autocontroles nuevos
    public EstadoOrden Estado { get; set; } = EstadoOrden.EnFabricacion;
    public DateTime? FechaFin { get; set; }
    public string? FinalizadaPor { get; set; }

    public List<Autocontrol> Autocontroles { get; set; } = [];
}
