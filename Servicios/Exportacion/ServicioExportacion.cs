using System.Text.RegularExpressions;
using VisualInspector.Datos;
using VisualInspector.Seguridad;

namespace VisualInspector.Servicios.Exportacion;

/// <summary>
/// En qué formato se descarga: libro de Excel (.xlsx) o CSV (texto con columnas separadas por «;»).
/// </summary>
public enum FormatoExportacion
{
    Excel,
    Csv
}

/// <summary>
/// Un archivo listo para descargar.
/// </summary>
public sealed record ArchivoExportado(byte[] Contenido, string Nombre, string TipoContenido);

/// <summary>
/// El archivo si ha ido bien, o el mensaje para el usuario si no se ha podido (por ejemplo, demasiadas filas).
/// </summary>
public sealed record ResultadoExportacion(ArchivoExportado? Archivo, string? Error);

/// <summary>
/// Prepara los archivos de Excel y CSV de Trazabilidad e Impacto productivo, con los mismos datos y filtros
/// que se ven en pantalla, para que Calidad pueda trabajarlos en Excel. Exige el permiso Autocontroles.Ver
/// (comprobado aquí y otra vez en los servicios de los que sale cada dato), revalida los filtros, pone un
/// tope de filas para que nadie bloquee el servidor y apunta cada exportación en el diario de seguridad.
/// </summary>
public sealed partial class ServicioExportacion(
    ServicioTrazabilidad trazabilidad,
    ServicioImpacto impacto,
    ServicioAutorizacion autorizacion,
    RegistroSeguridad registroSeguridad)
{
    // Topes de filas por archivo (se pueden bajar en las pruebas)
    public const int MaximoOrdenes_wys = 5000;
    public const int MaximoIncidencias_wys = 20000;

    public int MaximoOrdenes { get; init; } = MaximoOrdenes_wys;
    public int MaximoIncidencias { get; init; } = MaximoIncidencias_wys;

    private const string TipoExcel_wys = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string TipoCsv_wys = "text/csv; charset=utf-8";

    [GeneratedRegex("[^0-9A-Za-z]")]
    private static partial Regex CaracteresNoPermitidos();

    /// <summary>
    /// Exporta la lista de órdenes cerradas de Trazabilidad, con los mismos filtros de la pantalla.
    /// </summary>
    public async Task<ResultadoExportacion> ExportarOrdenesCerradasAsync(string? orden, string? litografia, FormatoExportacion formato)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesVer_wys);

        var filas = await trazabilidad.ListarCerradasAsync(orden, litografia, MaximoOrdenes + 1);
        if (filas.Count > MaximoOrdenes)
            return Error($"Hay más de {MaximoOrdenes:N0} órdenes que cumplen la búsqueda. Afina los filtros para exportar menos.");

        var tabla = new TablaExportable("Órdenes cerradas",
            ["Orden", "Litografía", "Línea", "Cerrada el", "Autocontroles completados", "Incidencias",
             "Eficacia de corrección (%)", "Minutos de parada", "Tubos retenidos"],
            filas.Select(f => new object?[]
            {
                f.Codigo, f.Litografia, f.Linea, f.FechaFin, f.ControlesCompletados, f.Incidencias,
                f.Eficacia, f.MinutosParada, f.TubosRetenidos
            }).ToList());

        var filtros = Filtros(("orden", orden), ("litografía", litografia));
        return await TerminarAsync($"Trazabilidad-ordenes-{DateTime.Now:yyyy-MM-dd}", formato, [tabla],
            $"órdenes cerradas de Trazabilidad{filtros}", filas.Count);
    }

    /// <summary>
    /// Exporta el cuadro de mando de una orden (varias hojas, así que solo en Excel).
    /// </summary>
    public async Task<ResultadoExportacion> ExportarTrazabilidadOrdenAsync(int id)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesVer_wys);

        var panel = await trazabilidad.ObtenerPanelAsync(id);
        if (panel is null)
            return Error("La orden ya no existe.");

        var resumen = new TablaExportable("Resumen", ["Concepto", "Valor"],
        [
            ["Orden", panel.Codigo],
            ["Litografía", panel.Litografia],
            ["Línea", panel.Linea],
            ["Estado", panel.EsProvisional ? "En fabricación (cifras provisionales)" : "Finalizada"],
            ["Inicio", $"{panel.Inicio:dd/MM/yyyy HH:mm}"],
            ["Fin", panel.FechaFin is { } fin ? $"{fin:dd/MM/yyyy HH:mm}" : null],
            ["Finalizada por", panel.FinalizadaPor],
            ["Autocontroles iniciados", panel.ControlesIniciados],
            ["Autocontroles completados", panel.ControlesCompletados],
            ["Muestras esperadas", panel.MuestrasEsperadas],
            ["Muestras escaneadas", panel.MuestrasEscaneadas],
            ["Incidencias detectadas", panel.Incidencias],
            ["Correcciones verificadas", panel.CorreccionesVerificadas],
            ["Eficacia de corrección (%)", panel.Eficacia],
            ["Autocontroles íntegros", panel.ControlesIntegros],
            ["Integridad de autocontroles (%)", panel.Integridad],
            ["Versiones del patrón", string.Join(", ", panel.VersionesPatron)]
        ]);

        var porMotivo = new TablaExportable("Incidencias por motivo", ["Motivo", "Detectadas", "Corregidas (verificadas)"],
            panel.PorMotivo.Select(m => new object?[] { ServicioImpacto.NombreDefecto(m.Motivo), m.Detectadas, m.Verificadas }).ToList());

        var i = panel.Impacto;
        var impactoTabla = new TablaExportable("Impacto productivo", ["Actuación en producción", "Incidencias"],
        [
            ["Sin intervención", i.SinIntervencion],
            ["Ajuste en marcha", i.AjusteEnMarcha],
            ["Parada de línea", i.ParadaLinea],
            ["Retención de producto", i.RetencionProducto],
            ["Aviso a Calidad", i.AvisoCalidad],
            ["Sin clasificar", i.SinClasificar],
            ["Minutos de parada (aprox.)", i.MinutosParada],
            ["Tubos retenidos (aprox.)", i.TubosRetenidos]
        ]);

        var tramos = new TablaExportable("Producción a revisar", ["Desde", "Fecha desde", "Hasta", "Fecha hasta"],
            panel.TramosARevisar.Select(t => new object?[]
            {
                ServicioAutocontroles.NombreControl(t.ControlDesde), t.Desde,
                ServicioAutocontroles.NombreControl(t.ControlHasta), t.Hasta
            }).ToList());

        var controles = new TablaExportable("Autocontroles",
            ["Autocontrol", "Fecha y hora", "Estado", "Resultado", "Incidencias", "Versión del patrón"],
            panel.Controles.Select(c => new object?[]
            {
                ServicioAutocontroles.NombreControl(c.NumeroControl), c.FechaControl,
                ServicioAutocontroles.NombreEstado(c.Estado),
                c.Estado == EstadoAutocontrol.Registrado ? ServicioAutocontroles.NombreResultado(c.Resultado) : "",
                c.Incidencias, c.VersionPatron
            }).ToList());

        var nombre = $"Trazabilidad-{panel.Codigo}{(panel.EsProvisional ? "-PROVISIONAL" : "")}";
        return await TerminarAsync(nombre, FormatoExportacion.Excel, [resumen, porMotivo, impactoTabla, tramos, controles],
            $"trazabilidad de la orden {panel.Codigo}", panel.Controles.Count);
    }

    /// <summary>
    /// Exporta el impacto productivo del periodo y la línea indicados (los mismos que se ven en pantalla).
    /// En Excel, todas las hojas; en CSV, solo el detalle de incidencias.
    /// </summary>
    public async Task<ResultadoExportacion> ExportarImpactoAsync(DateTime desde, DateTime hasta, string? linea, FormatoExportacion formato)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesVer_wys);

        if (hasta < desde) (desde, hasta) = (hasta, desde);
        var detalle = await impacto.ListarIncidenciasAsync(desde, hasta, linea, MaximoIncidencias);
        if (detalle.EstaRecortado)
            return Error($"Hay más de {MaximoIncidencias:N0} incidencias en ese periodo. Acorta las fechas o elige una línea.");

        var tablaDetalle = new TablaExportable("Detalle de incidencias",
            ["Orden", "Línea", "Litografía", "Autocontrol", "Fecha y hora del control", "Nº de diferencia", "Motivo",
             "Qué se hizo", "Minutos de parada", "Tubos retenidos", "Comentario", "Registrado por"],
            detalle.Filas.Select(f => new object?[]
            {
                f.Orden, f.Linea, f.Litografia, ServicioAutocontroles.NombreControl(f.NumeroControl), f.FechaControl,
                f.NumeroDiferencia, f.Motivo, f.Actuaciones, f.MinutosParada, f.TubosRetenidos, f.Comentario, f.RegistradoPor
            }).ToList());

        var periodo = $"{desde:yyyy-MM-dd}_{hasta:yyyy-MM-dd}";
        var lineaNombre = CaracteresNoPermitidos().Replace(linea ?? "", "");
        var sufijo = lineaNombre.Length > 0 ? $"-{lineaNombre}" : "";
        var filtros = Filtros(("desde", $"{desde:dd/MM/yyyy}"), ("hasta", $"{hasta:dd/MM/yyyy}"), ("línea", linea));

        if (formato == FormatoExportacion.Csv)
        {
            return await TerminarAsync($"Impacto-incidencias-{periodo}{sufijo}", formato, [tablaDetalle],
                $"detalle de incidencias de Impacto productivo{filtros}", detalle.Filas.Count);
        }

        var panel = await impacto.ObtenerAsync(desde, hasta, linea, MaximoOrdenes + 1);
        if (panel.Ordenes.Count > MaximoOrdenes)
            return Error($"Hay más de {MaximoOrdenes:N0} órdenes con incidencias en ese periodo. Acorta las fechas o elige una línea.");

        var resumen = new TablaExportable("Resumen", ["Concepto", "Valor"],
        [
            ["Desde", $"{panel.Desde:dd/MM/yyyy}"],
            ["Hasta", $"{panel.Hasta:dd/MM/yyyy}"],
            ["Línea", panel.Linea ?? "Todas"],
            ["Órdenes con incidencias", panel.OrdenesConIncidencias],
            ["Incidencias", panel.Incidencias],
            ["Sin intervención", panel.SinIntervencion],
            ["Con parada de línea", panel.ConParada],
            ["Minutos de parada (aprox.)", panel.Minutos],
            ["Con retención de producto", panel.ConRetencion],
            ["Tubos retenidos (aprox.)", panel.Tubos],
            ["Avisos a Calidad", panel.AvisosCalidad],
            ["Sin clasificar", panel.SinClasificar]
        ]);

        return await TerminarAsync($"Impacto-{periodo}{sufijo}", formato,
            [resumen,
             Ranking("Por línea", "Línea", panel.PorLinea),
             Ranking("Por litografía", "Litografía", panel.PorLitografia),
             Ranking("Por defecto", "Defecto", panel.PorDefecto),
             Ranking("Órdenes", "Orden", panel.Ordenes),
             tablaDetalle],
            $"impacto productivo{filtros}", detalle.Filas.Count);
    }

    /// <summary>
    /// Tabla de un ranking de impacto: de más a menos coste, como en pantalla.
    /// </summary>
    private static TablaExportable Ranking(string hoja, string columna, IReadOnlyList<FilaImpacto> filas) =>
        new(hoja, [columna, "Incidencias", "Con parada", "Minutos de parada", "Con retención", "Tubos retenidos"],
            filas.Select(f => new object?[] { f.Nombre, f.Incidencias, f.ConParada, f.Minutos, f.ConRetencion, f.Tubos }).ToList());

    /// <summary>
    /// Convierte las tablas en el archivo del formato elegido, apunta la exportación en el diario de
    /// seguridad y devuelve el resultado. El CSV solo admite una tabla.
    /// </summary>
    private async Task<ResultadoExportacion> TerminarAsync(
        string nombreSinExtension, FormatoExportacion formato, IReadOnlyList<TablaExportable> tablas, string descripcion, int filas)
    {
        var archivo = formato == FormatoExportacion.Csv
            ? new ArchivoExportado(EscritorExportacion.Csv(tablas[0]), nombreSinExtension + ".csv", TipoCsv_wys)
            : new ArchivoExportado(EscritorExportacion.Excel(tablas), nombreSinExtension + ".xlsx", TipoExcel_wys);

        // En el diario va qué se exportó y con qué filtros, nunca el contenido
        await registroSeguridad.RegistrarAsync(AccionesSeguridad.ExportacionDatos_wys,
            $"Correcto: {descripcion} ({(formato == FormatoExportacion.Csv ? "CSV" : "Excel")}), {filas} fila(s)");
        return new ResultadoExportacion(archivo, null);
    }

    private static ResultadoExportacion Error(string mensaje) => new(null, mensaje);

    /// <summary>
    /// Describe los filtros usados para el diario: solo letras y cifras de los códigos, sin nada raro.
    /// </summary>
    private static string Filtros(params (string Nombre, string? Valor)[] filtros)
    {
        var usados = filtros
            .Where(f => !string.IsNullOrWhiteSpace(f.Valor))
            .Select(f => $"{f.Nombre} {Regex.Replace(f.Valor!, @"[^0-9A-Za-z/]", "")}")
            .ToList();
        return usados.Count == 0 ? "" : $" [{string.Join(", ", usados)}]";
    }
}
