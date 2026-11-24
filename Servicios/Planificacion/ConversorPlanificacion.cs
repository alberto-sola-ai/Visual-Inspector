using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace VisualInspector.Servicios.Planificacion;

/// <summary>
/// Resultado de convertir las filas de la hoja: las órdenes válidas, cuántas filas no se han podido
/// usar y si se encontraron las columnas.
/// </summary>
/// <param name="TieneCabecera">Se encontró la fila de títulos con LINEA, PRIORIDAD y OF.</param>
/// <param name="FechaCargaHoja">Cuándo cargó producción los datos en la hoja («Carga: …» encima de
/// los títulos), o null si no viene.</param>
public sealed record ResultadoConversion(IReadOnlyList<OrdenPlanificada> Ordenes, int FilasDescartadas, bool TieneCabecera, DateTime? FechaCargaHoja = null);

/// <summary>
/// Convierte las filas de la hoja de planificación en órdenes que la aplicación entiende. No
/// habla con Google: recibe las celdas ya leídas, así se puede probar con datos de ejemplo.
/// Lo que viene de la hoja no es de fiar (Zero Trust): cada dato se comprueba y la fila que no
/// encaja se descarta sin romper el resto.
/// </summary>
public static partial class ConversorPlanificacion
{
    // Títulos de las columnas tal como están en la hoja, ya normalizados (mayúsculas, sin tildes)
    public const string ColumnaLinea_wys = "LINEA";
    public const string ColumnaDescripcion_wys = "DESCRIPCION";
    public const string ColumnaPrioridad_wys = "PRIORIDAD";
    public const string ColumnaPendientes_wys = "PENDIENTE DE FABRICAR";
    public const string ColumnaFabricados_wys = "FABRICADOS";
    public const string ColumnaCantidad_wys = "CANTIDAD";
    public const string ColumnaOrden_wys = "OF";
    public const string ColumnaEspecialidad_wys = "ESPECIALIDAD";

    // La fila de títulos se busca solo entre las primeras filas de la hoja
    private const int FilasDondeBuscarCabecera_wys = 10;
    private const int LongitudMaximaDescripcion_wys = 200;
    private const int LongitudMaximaEspecialidad_wys = 30;

    [GeneratedRegex("[0-9]+")]
    private static partial Regex Cifras();

    /// <summary>
    /// Lee las filas de la hoja y devuelve las órdenes válidas, ordenadas por línea (en el orden del
    /// parámetro Lineas) y, dentro de cada línea, por prioridad. Es como pasar a limpio una lista
    /// escrita a mano: se copia lo que se entiende y se aparta lo que no.
    /// </summary>
    /// <param name="filas">Celdas de la hoja, fila a fila, tal como las da Google.</param>
    /// <param name="lineasValidas">Líneas de la aplicación (parámetro Lineas).</param>
    /// <param name="prefijoOrden">Inicio habitual de las órdenes (parámetro PrefijoOrden, "10000"),
    /// para completar alguna orden que viniera sin él.</param>
    public static ResultadoConversion Convertir(IList<IList<object>>? filas, IReadOnlyList<string> lineasValidas, string prefijoOrden)
    {
        filas ??= [];

        // Primero se busca la fila de títulos: las columnas se localizan por su nombre, no por su
        // posición, para que mover una columna en la hoja no cambie los datos
        var indiceCabecera = -1;
        Dictionary<string, int> columnas = [];
        for (var i = 0; i < Math.Min(filas.Count, FilasDondeBuscarCabecera_wys); i++)
        {
            columnas = LocalizarColumnas(filas[i]);
            if (columnas.ContainsKey(ColumnaLinea_wys) && columnas.ContainsKey(ColumnaPrioridad_wys) && columnas.ContainsKey(ColumnaOrden_wys))
            {
                indiceCabecera = i;
                break;
            }
        }
        if (indiceCabecera < 0)
        {
            return new([], 0, false);
        }

        var ordenes = new List<OrdenPlanificada>();
        var codigosVistos = new HashSet<string>();
        var descartadas = 0;
        foreach (var fila in filas.Skip(indiceCabecera + 1))
        {
            var textoLinea = Texto(Celda(fila, columnas, ColumnaLinea_wys));
            var textoOrden = Texto(Celda(fila, columnas, ColumnaOrden_wys));

            // Las filas sin línea ni orden son huecos de la hoja: no cuentan como error
            if (textoLinea.Length == 0 && textoOrden.Length == 0)
            {
                continue;
            }

            var linea = TraducirLinea(textoLinea, lineasValidas);
            var codigo = CompletarOrden(textoOrden, prefijoOrden);
            var prioridad = Entero(Celda(fila, columnas, ColumnaPrioridad_wys));

            // Una misma orden solo se cuenta una vez (la primera que aparece)
            if (linea is null || codigo is null || prioridad is null or < 1 || !codigosVistos.Add(codigo))
            {
                descartadas++;
                continue;
            }

            ordenes.Add(new OrdenPlanificada(
                linea, prioridad.Value, codigo,
                Recortado(Texto(Celda(fila, columnas, ColumnaDescripcion_wys)), LongitudMaximaDescripcion_wys),
                NoNegativo(Entero(Celda(fila, columnas, ColumnaFabricados_wys))),
                NoNegativo(Entero(Celda(fila, columnas, ColumnaPendientes_wys))),
                NoNegativo(Entero(Celda(fila, columnas, ColumnaCantidad_wys))),
                // Columna añadida a la hoja el 24/11/2026: si no está, simplemente no se muestra
                Recortado(Texto(Celda(fila, columnas, ColumnaEspecialidad_wys)), LongitudMaximaEspecialidad_wys)));
        }

        var ordenLineas = lineasValidas.Select((l, i) => (l, i)).ToDictionary(x => x.l, x => x.i, StringComparer.OrdinalIgnoreCase);
        var ordenadas = ordenes
            .OrderBy(o => ordenLineas[o.Linea])
            .ThenBy(o => o.Prioridad)
            .ThenBy(o => o.Codigo)
            .ToList();
        return new(ordenadas, descartadas, true, BuscarFechaCarga(filas.Take(indiceCabecera)));
    }

    /// <summary>
    /// Busca encima de los títulos la celda «Carga: 24/11/2026 08:00:00», que dice cuándo se
    /// cargaron los datos en la hoja. Devuelve null si no está o no se entiende la fecha.
    /// </summary>
    private static DateTime? BuscarFechaCarga(IEnumerable<IList<object>> filasDeEncima)
    {
        const string Etiqueta = "Carga:";
        string[] formatos = ["dd/MM/yyyy HH:mm:ss", "d/M/yyyy H:mm:ss", "dd/MM/yyyy HH:mm", "d/M/yyyy H:mm"];
        foreach (var texto in filasDeEncima.SelectMany(f => f).Select(Texto))
        {
            if (texto.StartsWith(Etiqueta, StringComparison.OrdinalIgnoreCase)
                && DateTime.TryParseExact(texto[Etiqueta.Length..].Trim(), formatos, CultureInfo.GetCultureInfo("es-ES"), DateTimeStyles.None, out var fecha))
            {
                return fecha;
            }
        }
        return null;
    }

    /// <summary>
    /// Traduce la línea de la hoja («LINEA 1», «Línea 2», «L3»…) a la de la aplicación (L1, L2, L3):
    /// se queda con el número. Devuelve null si no hay número o esa línea no existe en la aplicación.
    /// </summary>
    public static string? TraducirLinea(string texto, IReadOnlyList<string> lineasValidas)
    {
        var numero = Cifras().Match(texto);
        if (!numero.Success || !int.TryParse(numero.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
        {
            return null;
        }
        return lineasValidas.FirstOrDefault(l => string.Equals(l, $"L{n}", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Pone la orden de la hoja en el formato de la aplicación (10 cifras). Normalmente la hoja ya la
    /// trae completa («1000063961») y se deja igual. Por si alguna fila viene sin el inicio habitual
    /// («63961»), se le pone delante el prefijo y, si faltan cifras, ceros en medio
    /// («10000» + «63961» = «1000063961»). Devuelve null si no son solo cifras o no cabe en 10.
    /// </summary>
    public static string? CompletarOrden(string texto, string prefijoOrden)
    {
        var cifras = new string(texto.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (cifras.Length == 0 || !cifras.All(char.IsAsciiDigit))
        {
            return null;
        }
        if (cifras.Length == ServicioAutocontroles.CifrasOrden_wys)
        {
            return cifras;
        }

        var hueco = ServicioAutocontroles.CifrasOrden_wys - prefijoOrden.Length;
        if (prefijoOrden.Length == 0 || cifras.Length > hueco)
        {
            return null;
        }
        return prefijoOrden + cifras.PadLeft(hueco, '0');
    }

    /// <summary>
    /// Apunta en qué posición está cada título de columna. Si un título se repite (la hoja tiene
    /// dos PRIORIDAD con el mismo valor), vale el primero.
    /// </summary>
    private static Dictionary<string, int> LocalizarColumnas(IList<object> fila)
    {
        var columnas = new Dictionary<string, int>();
        for (var i = 0; i < fila.Count; i++)
        {
            columnas.TryAdd(Normalizar(Texto(fila[i])), i);
        }
        return columnas;
    }

    /// <summary>
    /// Deja un título listo para comparar: sin tildes, en mayúsculas y con un solo espacio entre palabras.
    /// </summary>
    private static string Normalizar(string texto)
    {
        var sinTildes = new string(texto.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        return string.Join(' ', sinTildes.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Devuelve la celda de una columna en esta fila, o null si la fila es más corta o no existe la columna.
    /// </summary>
    private static object? Celda(IList<object> fila, Dictionary<string, int> columnas, string columna) =>
        columnas.TryGetValue(columna, out var i) && i < fila.Count ? fila[i] : null;

    /// <summary>
    /// Convierte una celda en texto. Los números enteros (Google los da como 63961 o 63961.0) se
    /// escriben sin decimales para no perder ni añadir cifras.
    /// </summary>
    private static string Texto(object? celda) => celda switch
    {
        null => "",
        double d when d == Math.Floor(d) && Math.Abs(d) < 1e15 => ((long)d).ToString(CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture).Trim(),
        _ => celda.ToString()?.Trim() ?? ""
    };

    /// <summary>
    /// Convierte una celda en número entero, o null si no lo es. Acepta números de Google y textos
    /// con formato español («1.234»).
    /// </summary>
    private static int? Entero(object? celda)
    {
        switch (celda)
        {
            case long l when l is >= int.MinValue and <= int.MaxValue:
                return (int)l;
            case int i:
                return i;
            case double d when d == Math.Floor(d) && d is >= int.MinValue and <= int.MaxValue:
                return (int)d;
            case string s when int.TryParse(s.Trim(), NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign,
                CultureInfo.GetCultureInfo("es-ES"), out var n):
                return n;
            default:
                return null;
        }
    }

    /// <summary>
    /// Un texto de la hoja, cortado a su largo máximo para que nada desmesurado llegue a la pantalla,
    /// o null si viene vacío.
    /// </summary>
    private static string? Recortado(string texto, int largoMaximo) =>
        texto.Length == 0 ? null : texto[..Math.Min(texto.Length, largoMaximo)];

    /// <summary>
    /// Las cantidades de tubos no pueden ser negativas: si lo son, se tratan como dato que falta.
    /// </summary>
    private static int? NoNegativo(int? valor) => valor is < 0 ? null : valor;
}
