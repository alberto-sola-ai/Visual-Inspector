using System.Data;
using System.Globalization;
using System.Text;
using MiniExcelLibs;
using MiniExcelLibs.Attributes;
using MiniExcelLibs.OpenXml;

namespace VisualInspector.Servicios.Exportacion;

/// <summary>
/// Una tabla lista para exportar: un nombre (el de la hoja de Excel), sus columnas y sus filas.
/// Cada valor puede ser texto, número, fecha, Sí/No (bool) o vacío (null).
/// </summary>
public sealed record TablaExportable(string Nombre, IReadOnlyList<string> Columnas, IReadOnlyList<object?[]> Filas);

/// <summary>
/// Convierte tablas en un archivo CSV o en un libro de Excel, como quien pasa una tabla del papel a otro
/// formato. El CSV va pensado para el Excel en español (punto y coma, UTF-8 con marca, coma decimal y fechas
/// dd/MM/aaaa). Todo texto se limpia antes: si empieza por =, +, - o @ Excel podría tomarlo por una fórmula
/// (y los comentarios los escriben los operarios), así que se le pone una comilla delante.
/// </summary>
public static class EscritorExportacion
{
    private static readonly CultureInfo Espana_wys = new("es-ES");
    private const string FormatoFecha_wys = "dd/MM/yyyy HH:mm";
    private const char Separador_wys = ';';

    // Caracteres con los que Excel empieza a leer una fórmula
    private static readonly char[] InicioFormula_wys = ['=', '+', '-', '@', '\t', '\r'];

    // Caracteres que Excel no admite en el nombre de una hoja
    private static readonly char[] NombreHojaNoValido_wys = ['\\', '/', '?', '*', '[', ']', ':'];

    /// <summary>
    /// Quita el peligro de un texto: si empieza como una fórmula, le antepone una comilla simple.
    /// </summary>
    public static string Limpiar(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
            return "";
        return texto.IndexOfAny(InicioFormula_wys) == 0 ? "'" + texto : texto;
    }

    /// <summary>
    /// Escribe una sola tabla en CSV: UTF-8 con marca de orden de bytes (para que Excel muestre bien los
    /// acentos), campos separados por «;» y filas separadas por salto de línea.
    /// </summary>
    public static byte[] Csv(TablaExportable tabla)
    {
        var texto = new StringBuilder();
        texto.AppendJoin(Separador_wys, tabla.Columnas.Select(c => CampoCsv(Limpiar(c)))).Append("\r\n");
        foreach (var fila in tabla.Filas)
        {
            texto.AppendJoin(Separador_wys, fila.Select(v => CampoCsv(ValorCsv(v)))).Append("\r\n");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(texto.ToString())).ToArray();
    }

    /// <summary>
    /// Escribe varias tablas en un libro de Excel (.xlsx), una por hoja. Los números y las fechas van
    /// como números y fechas de verdad, para poder sumarlos y ordenarlos.
    /// </summary>
    public static byte[] Excel(IReadOnlyList<TablaExportable> hojas)
    {
        var libro = new Dictionary<string, object>();
        foreach (var hoja in hojas)
        {
            libro[NombreHoja(hoja.Nombre, libro.Keys)] = ADataTable(hoja);
        }

        // El formato de fecha se indica por columna: las que llevan fechas se escriben como dd/MM/aaaa HH:mm
        var columnasConFecha = hojas
            .SelectMany(h => h.Columnas.Where((_, j) => h.Filas.Any(f => f[j] is DateTime)))
            .Distinct()
            .Select(c => new DynamicExcelColumn(Limpiar(c)) { Format = FormatoFecha_wys })
            .ToArray();

        using var flujo = new MemoryStream();
        flujo.SaveAs(libro, configuration: new OpenXmlConfiguration
        {
            DynamicColumns = columnasConFecha,
            EnableAutoWidth = true,
            FastMode = true      // el ancho automático de las columnas lo exige
        });
        return flujo.ToArray();
    }

    /// <summary>
    /// Pasa la tabla a un DataTable (lo que entiende la biblioteca de Excel), limpiando los textos.
    /// </summary>
    private static DataTable ADataTable(TablaExportable tabla)
    {
        var resultado = new DataTable(tabla.Nombre);
        foreach (var columna in tabla.Columnas)
        {
            resultado.Columns.Add(Limpiar(columna), typeof(object));
        }
        foreach (var fila in tabla.Filas)
        {
            resultado.Rows.Add(fila.Select(ValorExcel).ToArray());
        }
        return resultado;
    }

    /// <summary>
    /// Valor para una celda de Excel: texto limpio, Sí/No en lugar de verdadero/falso, y números y fechas tal cual.
    /// </summary>
    private static object ValorExcel(object? valor) => valor switch
    {
        null => DBNull.Value,
        string texto => Limpiar(texto),
        bool si => si ? "Sí" : "No",
        _ => valor
    };

    /// <summary>
    /// Valor para una celda de CSV, escrito como lo espera el Excel en español.
    /// </summary>
    private static string ValorCsv(object? valor) => valor switch
    {
        null => "",
        string texto => Limpiar(texto),
        bool si => si ? "Sí" : "No",
        DateTime fecha => fecha.ToString(FormatoFecha_wys, Espana_wys),
        decimal numero => numero.ToString("0.##", Espana_wys),
        double numero => numero.ToString("0.##", Espana_wys),
        float numero => numero.ToString("0.##", Espana_wys),
        IFormattable numero => numero.ToString(null, Espana_wys),
        _ => Limpiar(valor.ToString())
    };

    /// <summary>
    /// Entrecomilla un campo de CSV si lleva punto y coma, comillas o saltos de línea.
    /// </summary>
    private static string CampoCsv(string campo) =>
        campo.IndexOfAny([Separador_wys, '"', '\r', '\n']) >= 0
            ? "\"" + campo.Replace("\"", "\"\"") + "\""
            : campo;

    /// <summary>
    /// Nombre válido para una hoja de Excel: sin caracteres prohibidos, de 31 letras como mucho y sin repetir.
    /// </summary>
    private static string NombreHoja(string nombre, IEnumerable<string> yaUsados)
    {
        var limpio = string.Concat(nombre.Select(c => NombreHojaNoValido_wys.Contains(c) ? ' ' : c)).Trim();
        if (limpio.Length == 0) limpio = "Hoja";
        if (limpio.Length > 31) limpio = limpio[..31];
        var usados = yaUsados.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidato = limpio;
        for (var n = 2; usados.Contains(candidato); n++)
        {
            var sufijo = $" {n}";
            candidato = limpio[..Math.Min(limpio.Length, 31 - sufijo.Length)] + sufijo;
        }
        return candidato;
    }
}
