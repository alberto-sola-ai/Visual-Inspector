using System.Globalization;

namespace VisualInspector.Servicios;

/// <summary>
/// Escribe las cifras de las pantallas con el formato español: "94,4 %" y "100 %".
/// </summary>
public static class FormatoCifras
{
    /// <summary>
    /// Escribe un porcentaje con un decimal solo si hace falta: 100 se queda en "100 %" y 94,44 pasa a "94,4 %".
    /// </summary>
    public static string Porcentaje(decimal valor) =>
        valor == Math.Round(valor)
            ? string.Create(CultureInfo.CurrentCulture, $"{valor:0} %")
            : string.Create(CultureInfo.CurrentCulture, $"{valor:0.0} %");

    /// <summary>
    /// Escribe un porcentaje para un ancho de barra en CSS: siempre con punto decimal y entre 0 y 100.
    /// </summary>
    public static string AnchoBarra(decimal valor) =>
        Math.Clamp(valor, 0m, 100m).ToString("0.0", CultureInfo.InvariantCulture);
}
