using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using VisualInspector.Datos;
using VisualInspector.Servicios.Vision;

namespace VisualInspector.Servicios.Informes;

/// <summary>
/// Le da a la biblioteca de PDF la tipografía corporativa Lato (archivos TTF de recursos/fuentes),
/// porque en el servidor no hay fuentes instaladas.
/// </summary>
internal sealed class ResolutorFuentesLato(string carpetaFuentes) : IFontResolver
{
    public const string Familia_wys = "Lato";
    public const string FamiliaNegrita_wys = "Lato Black";

    /// <summary>
    /// Elige el archivo de Lato que corresponde: normal, negrita o "black" (la más gruesa).
    /// La cursiva no se usa en el informe; si se pide, se usa la normal.
    /// </summary>
    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (familyName.Equals(FamiliaNegrita_wys, StringComparison.OrdinalIgnoreCase))
            return new FontResolverInfo("Lato-Black");
        return new FontResolverInfo(isBold ? "Lato-Bold" : "Lato-Regular");
    }

    /// <summary>
    /// Devuelve el contenido del archivo de fuente pedido.
    /// </summary>
    public byte[] GetFont(string faceName) => File.ReadAllBytes(Path.Combine(carpetaFuentes, $"{faceName}.ttf"));
}

/// <summary>
/// Genera el informe de fabricación en PDF con la identidad visual WYS: título en mayúsculas y
/// azul oscuro, logo arriba a la derecha, tipografía Lato y pie azul con "WITTE Y SOLÀ, S.A.".
/// Es la versión para archivar o enviar con la documentación de calidad.
/// </summary>
public sealed class GeneradorInformePdf
{
    // Colores de la marca (los mismos que recursos/tema.css)
    private static readonly Color AzulOscuro_wys = new(0x00, 0x31, 0x60);
    private static readonly Color Rojo_wys = new(0xE2, 0x23, 0x1A);
    private static readonly Color GrisClaro_wys = new(0xF4, 0xF4, 0xF3);
    private static readonly Color GrisTexto_wys = new(0x5F, 0x5F, 0x5F);

    private static readonly Lock CerrojoFuentes_wys = new();
    private readonly string carpetaRecursos;

    /// <summary>
    /// Prepara el generador y registra la tipografía Lato (una sola vez para toda la aplicación).
    /// </summary>
    public GeneradorInformePdf(IWebHostEnvironment entorno)
    {
        carpetaRecursos = Path.Combine(entorno.ContentRootPath, "recursos");
        lock (CerrojoFuentes_wys)
        {
            GlobalFontSettings.FontResolver ??= new ResolutorFuentesLato(Path.Combine(carpetaRecursos, "fuentes"));
        }
    }

    /// <summary>
    /// Crea el PDF del informe y lo devuelve como bytes, listo para descargar.
    /// </summary>
    public byte[] Generar(InformeFabricacion informe)
    {
        var documento = new Document();
        documento.Info.Title = $"Informe de fabricación · Orden {informe.Orden.Codigo}";
        documento.Info.Author = "Visual Inspector · Witte y Solà, S.A.";
        DefinirEstilos(documento);

        var seccion = documento.AddSection();
        seccion.PageSetup = documento.DefaultPageSetup.Clone();
        seccion.PageSetup.PageFormat = PageFormat.A4;
        seccion.PageSetup.TopMargin = Unit.FromCentimeter(3);
        seccion.PageSetup.BottomMargin = Unit.FromCentimeter(2.2);
        seccion.PageSetup.LeftMargin = Unit.FromCentimeter(2);
        seccion.PageSetup.RightMargin = Unit.FromCentimeter(2);

        Cabecera(seccion, informe);
        Pie(seccion, informe);
        Datos(seccion, informe);
        Resumen(seccion, informe);
        Autocontroles(seccion, informe);
        Incidencias(seccion, informe);

        var renderizador = new PdfDocumentRenderer { Document = documento };
        renderizador.RenderDocument();
        using var memoria = new MemoryStream();
        renderizador.PdfDocument.Save(memoria);
        return memoria.ToArray();
    }

    /// <summary>
    /// Tipografía y tamaños del informe: Lato en todo; títulos en mayúsculas y azul oscuro.
    /// </summary>
    private static void DefinirEstilos(Document documento)
    {
        var normal = documento.Styles[StyleNames.Normal]!;
        normal.Font.Name = ResolutorFuentesLato.Familia_wys;
        normal.Font.Size = 9;

        var titulo1 = documento.Styles[StyleNames.Heading1]!;
        titulo1.Font.Name = ResolutorFuentesLato.FamiliaNegrita_wys;
        titulo1.Font.Size = 13;
        titulo1.Font.Color = AzulOscuro_wys;
        titulo1.ParagraphFormat.SpaceBefore = Unit.FromPoint(14);
        titulo1.ParagraphFormat.SpaceAfter = Unit.FromPoint(6);
        titulo1.ParagraphFormat.KeepWithNext = true;

        var titulo2 = documento.Styles[StyleNames.Heading2]!;
        titulo2.Font.Name = ResolutorFuentesLato.Familia_wys;
        titulo2.Font.Bold = true;
        titulo2.Font.Size = 10;
        titulo2.Font.Color = AzulOscuro_wys;
        titulo2.ParagraphFormat.SpaceBefore = Unit.FromPoint(10);
        titulo2.ParagraphFormat.SpaceAfter = Unit.FromPoint(3);
        titulo2.ParagraphFormat.KeepWithNext = true;
    }

    /// <summary>
    /// Cabecera de cada página: título del documento en mayúsculas a la izquierda y logo WYS a la derecha.
    /// </summary>
    private void Cabecera(Section seccion, InformeFabricacion informe)
    {
        var tabla = seccion.Headers.Primary.AddTable();
        tabla.AddColumn(Unit.FromCentimeter(13));
        tabla.AddColumn(Unit.FromCentimeter(4));
        var fila = tabla.AddRow();

        var titulo = fila.Cells[0].AddParagraph();
        titulo.AddFormattedText("INFORME DE FABRICACIÓN", new Font(ResolutorFuentesLato.FamiliaNegrita_wys, 15) { Color = AzulOscuro_wys });
        var subtitulo = fila.Cells[0].AddParagraph($"Orden {informe.Orden.Codigo} · Litografía {informe.Orden.Litografia ?? "-"}");
        subtitulo.Format.Font.Color = GrisTexto_wys;
        if (informe.EsProvisional)
        {
            var provisional = fila.Cells[0].AddParagraph();
            provisional.AddFormattedText("PROVISIONAL · la orden aún no está finalizada", new Font(ResolutorFuentesLato.Familia_wys, 9) { Bold = true, Color = Rojo_wys });
        }

        // Logo WYS (archivo PNG de la marca, sin modificarlo), arriba a la derecha
        var logo = fila.Cells[1].AddImage("base64:" + Convert.ToBase64String(File.ReadAllBytes(Path.Combine(carpetaRecursos, "marca", "wys-logo.png"))));
        logo.Width = Unit.FromCentimeter(2.6);
        logo.LockAspectRatio = true;
        fila.Cells[1].Format.Alignment = ParagraphAlignment.Right;

        // Filete fino horizontal bajo la cabecera
        tabla.Borders.Bottom.Width = 0.5;
        tabla.Borders.Bottom.Color = new Color(0x85, 0x97, 0xA3);
    }

    /// <summary>
    /// Pie de cada página: franja azul oscura con "WITTE Y SOLÀ, S.A." y el número de página.
    /// </summary>
    private static void Pie(Section seccion, InformeFabricacion informe)
    {
        var pie = seccion.Footers.Primary.AddParagraph();
        pie.Format.Shading.Color = AzulOscuro_wys;
        pie.Format.Font.Color = Colors.White;
        pie.Format.Font.Size = 8;
        pie.Format.Font.Bold = true;
        pie.Format.Alignment = ParagraphAlignment.Center;
        pie.Format.SpaceBefore = Unit.FromPoint(4);
        pie.AddText($"WITTE Y SOLÀ, S.A.   ·   Orden {informe.Orden.Codigo}   ·   Página ");
        pie.AddPageField();
        pie.AddText(" de ");
        pie.AddNumPagesField();
    }

    /// <summary>
    /// Datos de la orden en una tabla de dos columnas.
    /// </summary>
    private static void Datos(Section seccion, InformeFabricacion informe)
    {
        seccion.AddParagraph("Datos de la orden", StyleNames.Heading1);
        var o = informe.Orden;
        var primero = o.Autocontroles.FirstOrDefault()?.FechaControl;
        var ultimo = o.Autocontroles.LastOrDefault()?.FechaControl;
        var filas = new List<(string, string)>
        {
            ("Orden de fabricación", o.Codigo),
            ("Litografía", o.Litografia ?? "-"),
            ("Versiones del patrón", informe.VersionesPatron.Count > 0 ? string.Join(", ", informe.VersionesPatron) : "-"),
            ("Primer autocontrol", primero?.ToString("dd/MM/yyyy HH:mm") ?? "-"),
            ("Último autocontrol", ultimo?.ToString("dd/MM/yyyy HH:mm") ?? "-"),
            ("Estado", ServicioOrdenes.NombreEstado(o.Estado)),
            ("Finalizada", o.FechaFin is null ? "-" : $"{o.FechaFin:dd/MM/yyyy HH:mm} por {o.FinalizadaPor}"),
            ("Informe generado", $"{informe.Generado:dd/MM/yyyy HH:mm} por {informe.GeneradoPor}")
        };

        var tabla = seccion.AddTable();
        tabla.AddColumn(Unit.FromCentimeter(5));
        tabla.AddColumn(Unit.FromCentimeter(12));
        foreach (var (etiqueta, valor) in filas)
        {
            var fila = tabla.AddRow();
            fila.Cells[0].AddParagraph(etiqueta).Format.Font.Bold = true;
            fila.Cells[1].AddParagraph(valor);
            fila.Borders.Bottom.Width = 0.25;
            fila.Borders.Bottom.Color = new Color(0xD9, 0xD9, 0xD6);
            fila.TopPadding = Unit.FromPoint(2);
            fila.BottomPadding = Unit.FromPoint(2);
        }
    }

    /// <summary>
    /// Resumen en cajas: autocontroles, cuántos con incidencias, diferencias revisadas e incidencias.
    /// </summary>
    private static void Resumen(Section seccion, InformeFabricacion informe)
    {
        seccion.AddParagraph("Resumen", StyleNames.Heading1);
        var o = informe.Orden;
        var cajas = new (string Valor, string Texto, bool EsAviso)[]
        {
            (o.Autocontroles.Count(a => a.EsRegistradoValido).ToString(), "autocontroles registrados", false),
            (o.Autocontroles.Count(a => a.Cuenta && a.Resultado == ResultadoAutocontrol.ConIncidencias).ToString(), "autocontroles con incidencias", false),
            (informe.DiferenciasTotales.ToString(), "diferencias revisadas", false),
            (informe.Incidencias.ToString(), "incidencias", informe.Incidencias > 0)
        };

        var tabla = seccion.AddTable();
        for (var i = 0; i < cajas.Length; i++) tabla.AddColumn(Unit.FromCentimeter(4.25));
        var fila = tabla.AddRow();
        for (var i = 0; i < cajas.Length; i++)
        {
            var celda = fila.Cells[i];
            celda.Shading.Color = cajas[i].EsAviso ? Rojo_wys : AzulOscuro_wys;
            celda.Format.Font.Color = Colors.White;
            celda.Format.LeftIndent = Unit.FromPoint(6);
            var numero = celda.AddParagraph(cajas[i].Valor);
            numero.Format.Font.Name = ResolutorFuentesLato.FamiliaNegrita_wys;
            numero.Format.Font.Size = 18;
            numero.Format.SpaceBefore = Unit.FromPoint(4);
            celda.AddParagraph(cajas[i].Texto).Format.SpaceAfter = Unit.FromPoint(5);
            celda.Borders.Right.Width = 3;
            celda.Borders.Right.Color = Colors.White;
        }

        if (informe.Incidencias > 0)
        {
            var detalle = seccion.AddParagraph();
            detalle.Format.SpaceBefore = Unit.FromPoint(6);
            detalle.AddText("Incidencias por motivo: " + string.Join(" · ",
                informe.IncidenciasPorMotivo.Where(m => m.Value > 0).Select(m => $"{NombreMotivo(m.Key)}: {m.Value}")));
            detalle.AddText($". En las dos muestras (posible fallo de plancha): {informe.IncidenciasEnLasDos}.");
        }
    }

    /// <summary>
    /// Tabla con todos los autocontroles de la orden, del primero al último. Los anulados salen en
    /// gris, con su motivo debajo, porque no cuentan en el resto del informe.
    /// </summary>
    private static void Autocontroles(Section seccion, InformeFabricacion informe)
    {
        seccion.AddParagraph("Autocontroles", StyleNames.Heading1);
        var tabla = seccion.AddTable();
        var anchos = new[] { 2.6, 3.2, 1.4, 3.3, 1.8, 1.8, 2.9 };
        foreach (var ancho in anchos) tabla.AddColumn(Unit.FromCentimeter(ancho));

        var cabecera = tabla.AddRow();
        cabecera.HeadingFormat = true;   // se repite en cada página
        cabecera.Shading.Color = AzulOscuro_wys;
        cabecera.Format.Font.Color = Colors.White;
        cabecera.Format.Font.Bold = true;
        var titulos = new[] { "Control", "Fecha y hora", "Versión", "Resultado", "Diferencias", "Incidencias", "Registrado por" };
        for (var i = 0; i < titulos.Length; i++) cabecera.Cells[i].AddParagraph(titulos[i]);

        var alterna = false;
        foreach (var a in informe.Orden.Autocontroles)
        {
            var fila = tabla.AddRow();
            if (alterna) fila.Shading.Color = GrisClaro_wys;
            alterna = !alterna;
            fila.TopPadding = Unit.FromPoint(2);
            fila.BottomPadding = Unit.FromPoint(2);
            // Nombre del control y, debajo en pequeño, el tiempo real desde el primero
            fila.Cells[0].AddParagraph(ServicioAutocontroles.NombreControl(a.NumeroControl));
            var tiempo = ServicioAutocontroles.TiempoDesdeInicio(a.FechaControl, informe.Orden.FechaInicio);
            if (tiempo.Length > 0)
            {
                var parrafoTiempo = fila.Cells[0].AddParagraph(tiempo);
                parrafoTiempo.Format.Font.Size = 7.5;
                parrafoTiempo.Format.Font.Color = GrisTexto_wys;
            }
            fila.Cells[1].AddParagraph(a.FechaControl.ToString("dd/MM/yyyy HH:mm"));
            fila.Cells[2].AddParagraph(a.VersionPatron ?? "-");

            if (a.Anulacion is not null)
            {
                fila.Format.Font.Color = GrisTexto_wys;
                fila.Cells[3].AddParagraph("Anulado").Format.Font.Bold = true;
                fila.Cells[4].AddParagraph("-");
                fila.Cells[5].AddParagraph("-");
                fila.Cells[6].AddParagraph(a.RegistradoPor ?? "-");

                // Fila de debajo, a todo lo ancho, con el motivo y quién lo anuló
                var motivo = tabla.AddRow();
                if (!alterna) motivo.Shading.Color = GrisClaro_wys;
                motivo.Format.Font.Color = GrisTexto_wys;
                motivo.Format.Font.Size = 8;
                motivo.BottomPadding = Unit.FromPoint(3);
                motivo.Cells[0].MergeRight = anchos.Length - 1;
                var texto = motivo.Cells[0].AddParagraph();
                texto.AddFormattedText("Motivo de la anulación: ", TextFormat.Bold);
                texto.AddText($"{a.Anulacion.Motivo} (anulado por {a.Anulacion.AnuladoPor} el {a.Anulacion.Fecha:dd/MM/yyyy HH:mm})");
                continue;
            }

            var resultado = fila.Cells[3].AddParagraph(a.Estado == EstadoAutocontrol.Registrado
                ? ServicioAutocontroles.NombreResultado(a.Resultado) : "Sin registrar");
            if (a.Resultado == ResultadoAutocontrol.ConIncidencias)
            {
                resultado.Format.Font.Color = Rojo_wys;
                resultado.Format.Font.Bold = true;
            }
            fila.Cells[4].AddParagraph(a.Diferencias.ToString());
            fila.Cells[5].AddParagraph(a.Incidencias.ToString());
            fila.Cells[6].AddParagraph(a.RegistradoPor ?? "-");
        }
    }

    /// <summary>
    /// Detalle de cada incidencia: en qué control salió, motivo, comentario, quién la validó y su recorte.
    /// </summary>
    private static void Incidencias(Section seccion, InformeFabricacion informe)
    {
        seccion.AddParagraph("Incidencias", StyleNames.Heading1);
        if (informe.DetalleIncidencias.Count == 0)
        {
            seccion.AddParagraph("No hubo incidencias en esta orden.");
            return;
        }

        foreach (var i in informe.DetalleIncidencias)
        {
            seccion.AddParagraph(
                $"{ServicioAutocontroles.NombreControlConTiempo(i.NumeroControl, i.FechaControl, informe.Orden.FechaInicio)} ({i.FechaControl:dd/MM/yyyy HH:mm}) · diferencia n.º {i.Numero}",
                StyleNames.Heading2);

            var datos = seccion.AddParagraph();
            datos.Format.KeepWithNext = true;
            datos.AddFormattedText(NombreMotivo(i.Motivo), TextFormat.Bold);
            datos.AddText(i.EnMuestra1 && i.EnMuestra2 ? " · en las dos muestras (posible fallo de plancha)"
                : $" · solo en la muestra {(i.EnMuestra1 ? 1 : 2)}");
            datos.AddText($" · zona de {i.AnchoMm:N0} × {i.AltoMm:N0} mm");

            var comentario = seccion.AddParagraph();
            comentario.Format.KeepWithNext = true;
            comentario.AddFormattedText("Comentario: ", TextFormat.Bold);
            comentario.AddText(i.Comentario ?? "-");

            var validada = seccion.AddParagraph($"Validada por {i.ValidadoPor} el {i.FechaValidacion:dd/MM/yyyy HH:mm}");
            validada.Format.Font.Color = GrisTexto_wys;
            validada.Format.Font.Size = 8;
            validada.Format.KeepWithNext = true;

            // Recorte en color (patrón a la izquierda, muestra(s) a la derecha)
            var parrafoImagen = seccion.AddParagraph();
            parrafoImagen.Format.SpaceBefore = Unit.FromPoint(3);
            var imagen = parrafoImagen.AddImage("base64:" + Convert.ToBase64String(i.Recorte));
            imagen.Width = Unit.FromCentimeter(14);
            imagen.LockAspectRatio = true;
            var leyenda = seccion.AddParagraph("Patrón a la izquierda; muestra(s) a la derecha.");
            leyenda.Format.Font.Size = 7;
            leyenda.Format.Font.Color = GrisTexto_wys;
        }
    }

    private static string NombreMotivo(MotivoDiferencia motivo) => motivo switch
    {
        MotivoDiferencia.Mancha => "Mancha o suciedad",
        MotivoDiferencia.Falta => "Falta elemento o tinta",
        _ => "Desplazamiento o texto doble"
    };
}
