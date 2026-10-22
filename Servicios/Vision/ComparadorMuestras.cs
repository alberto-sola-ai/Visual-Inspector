using OpenCvSharp;
using VisualInspector.Seguridad;

namespace VisualInspector.Servicios.Vision;

/// <summary>
/// Tolerancia de la comparación, que el operario puede cambiar en cada autocontrol.
/// </summary>
public enum NivelTolerancia
{
    Estricta,
    Normal,
    Amplia
}

/// <summary>
/// Motivo de una diferencia, para agruparlas. Los pliegues y bordes se descartan y no aparecen.
/// </summary>
public enum MotivoDiferencia
{
    Mancha,
    Falta,
    Desplazamiento
}

/// <summary>
/// Una posible diferencia entre el patrón y las muestras.
/// </summary>
/// <param name="Numero">Número que la identifica en la vista general.</param>
/// <param name="Motivo">Mancha, falta o desplazamiento.</param>
/// <param name="EnMuestra1">Si aparece en la muestra 1.</param>
/// <param name="EnMuestra2">Si aparece en la muestra 2.</param>
/// <param name="XMm">Posición sobre el patrón (desde la esquina superior izquierda de la página), en mm.</param>
/// <param name="YMm">Posición sobre el patrón, en mm.</param>
/// <param name="AnchoMm">Ancho de la zona, en mm.</param>
/// <param name="AltoMm">Alto de la zona, en mm.</param>
/// <param name="AreaMm2">Superficie realmente distinta, en mm².</param>
/// <param name="Evidencia">Puntuación para ordenar: cuanto más alta, más evidente.</param>
/// <param name="Recorte">Imagen pequeña en color: patrón a la izquierda, muestra(s) a la derecha.</param>
public sealed record DiferenciaDetectada(
    int Numero, MotivoDiferencia Motivo, bool EnMuestra1, bool EnMuestra2,
    double XMm, double YMm, double AnchoMm, double AltoMm, double AreaMm2, double Evidencia, byte[] Recorte)
{
    public bool EnLasDos => EnMuestra1 && EnMuestra2;
}

/// <summary>
/// Resultado completo de comparar las dos muestras con el patrón.
/// </summary>
/// <param name="Tolerancia">Tolerancia usada.</param>
/// <param name="SimilitudMuestra1">Parecido patrón ↔ muestra 1 (%), solo orientativo.</param>
/// <param name="SimilitudMuestra2">Parecido patrón ↔ muestra 2 (%), solo orientativo.</param>
/// <param name="SimilitudEntreMuestras">Parecido muestra 1 ↔ muestra 2 (%), solo orientativo.</param>
/// <param name="Diferencias">Posibles diferencias, de la más evidente a la menos.</param>
/// <param name="VistaGeneral">Patrón con las diferencias numeradas.</param>
/// <param name="Avisos">Explicaciones para el usuario si algo no se ha podido comparar.</param>
public sealed record ResultadoComparacion(
    NivelTolerancia Tolerancia, double? SimilitudMuestra1, double? SimilitudMuestra2, double? SimilitudEntreMuestras,
    IReadOnlyList<DiferenciaDetectada> Diferencias, byte[]? VistaGeneral, IReadOnlyList<string> Avisos);

/// <summary>
/// Compara cada muestra (ya colocada encima del patrón) con el patrón y saca las posibles
/// diferencias de contenido: manchas, elementos o tinta que faltan y textos corridos o dobles.
/// No mide el tono del color. Es como poner el patrón y la muestra uno encima del otro a
/// contraluz y señalar lo que no coincide, pero con un margen para no fijarse en minucias.
/// </summary>
public sealed class ComparadorMuestras(ServicioAutorizacion autorizacion)
{
    // A 300 ppp, 1 mm son 11,81 píxeles
    private const double PixelesPorMm_wys = ServicioArchivosAutocontrol.Resolucion_wys / 25.4;
    private const ulong SemillaAzar_wys = 20261022;

    /// <summary>
    /// Compara las dos muestras con el patrón usando la tolerancia elegida.
    /// </summary>
    public async Task<ResultadoComparacion> CompararAsync(ArchivoPreparado patron, ArchivoPreparado muestra1, ArchivoPreparado muestra2, NivelTolerancia tolerancia)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);

        // Fuera del hilo de la pantalla y con tiempo máximo
        return await Task.Run(() =>
        {
            using var patronPreparado = PatronPreparado.Crear(patron.Contenido);
            using var imagen1 = ServicioArchivosAutocontrol.LeerImagen(muestra1.Contenido);
            using var imagen2 = ServicioArchivosAutocontrol.LeerImagen(muestra2.Contenido);
            return Comparar(patronPreparado, imagen1, imagen2, tolerancia);
        }).WaitAsync(TimeSpan.FromSeconds(LocalizadorMuestras.TiempoMaximoSegundos_wys));
    }

    /// <summary>
    /// Localiza las dos muestras, las compara con el patrón y junta las diferencias que salen en
    /// el mismo sitio en las dos (posible fallo de plancha).
    /// </summary>
    public static ResultadoComparacion Comparar(PatronPreparado patron, Mat imagen1, Mat imagen2, NivelTolerancia tolerancia)
    {
        // Algunos pasos (colocar la muestra, agrupar tintas) empiezan probando al azar. Con una
        // "semilla" fija, los mismos archivos dan siempre el mismo resultado (importante para el registro)
        Cv2.SetTheRNG(SemillaAzar_wys);

        var ajustes = AjustesTolerancia.De(tolerancia);
        var avisos = new List<string>();
        using var localizada1 = LocalizadorMuestras.Localizar(patron, imagen1);
        using var localizada2 = LocalizadorMuestras.Localizar(patron, imagen2);

        using var comparada1 = localizada1.Resultado.EsLocalizada ? CompararUna(patron.Imagen, localizada1, ajustes) : null;
        using var comparada2 = localizada2.Resultado.EsLocalizada ? CompararUna(patron.Imagen, localizada2, ajustes) : null;
        if (comparada1 is null) avisos.Add($"Muestra 1: {localizada1.Resultado.Aviso}");
        if (comparada2 is null) avisos.Add($"Muestra 2: {localizada2.Resultado.Aviso}");

        var diferencias = JuntarDiferencias(patron.Imagen, comparada1, comparada2);
        var similitudEntreMuestras = comparada1 is not null && comparada2 is not null
            ? SimilitudEntreMuestras(comparada1, comparada2)
            : (double?)null;
        var vista = diferencias.Count > 0 || comparada1 is not null || comparada2 is not null
            ? VistaGeneral(patron.Imagen, diferencias, comparada1, comparada2)
            : null;

        return new ResultadoComparacion(tolerancia, comparada1?.Similitud, comparada2?.Similitud, similitudEntreMuestras,
            diferencias.Select(d => d.Diferencia).ToList(), vista, avisos);
    }

    // ------------------------------------------------------------------------------------------
    // Ajustes de cada nivel de tolerancia
    // ------------------------------------------------------------------------------------------

    /// <summary>
    /// Lo que significa cada nivel de tolerancia, en números.
    /// </summary>
    /// <param name="AreaMinimaMm2">Una diferencia más pequeña que esto no se enseña.</param>
    /// <param name="DesajustePx">Margen de desajuste: lo que esté corrido menos de esto no cuenta.</param>
    /// <param name="ContrasteMinimo">Diferencia mínima de luminosidad (0-255) para tenerla en cuenta.</param>
    internal sealed record AjustesTolerancia(double AreaMinimaMm2, int DesajustePx, double ContrasteMinimo)
    {
        public static AjustesTolerancia De(NivelTolerancia nivel) => nivel switch
        {
            NivelTolerancia.Estricta => new(0.3, 2, 22),
            NivelTolerancia.Amplia => new(3.0, 7, 40),
            _ => new(1.0, 4, 24)
        };
    }

    // ------------------------------------------------------------------------------------------
    // Comparación de una muestra
    // ------------------------------------------------------------------------------------------

    /// <summary>
    /// Una diferencia de una sola muestra, en píxeles del patrón, antes de juntarla con la otra muestra.
    /// </summary>
    private sealed record DiferenciaEnMuestra(Rect Zona, MotivoDiferencia Motivo, double AreaMm2, double Contraste);

    /// <summary>
    /// Lo que queda de comparar una muestra: sus diferencias y su imagen colocada sobre el patrón.
    /// </summary>
    private sealed class MuestraComparada : IDisposable
    {
        public required Rect ZonaTrabajo { get; init; }           // zona de la página donde está la muestra
        public required Mat MuestraColor { get; init; }           // muestra colocada y ajustada (en esa zona)
        public required Mat MuestraGris { get; init; }            // luminosidad ajustada al patrón (en esa zona)
        public required Mat Interior { get; init; }               // zona útil (sin bordes de la pieza)
        public required List<DiferenciaEnMuestra> Diferencias { get; init; }
        public required double Similitud { get; init; }

        public void Dispose()
        {
            MuestraColor.Dispose();
            MuestraGris.Dispose();
            Interior.Dispose();
        }
    }

    /// <summary>
    /// Compara una muestra con el patrón: la coloca, corrige los pequeños desajustes por zonas,
    /// iguala la luminosidad general y busca lo que no coincide.
    /// </summary>
    private static MuestraComparada CompararUna(Mat patron, MuestraLocalizada localizada, AjustesTolerancia ajustes)
    {
        var (colocada, mascara) = localizada.ColocarEnPatron(patron.Size());
        using (colocada)
        using (mascara)
        {
            // Solo se trabaja en la zona de la página donde está la muestra
            var zona = Cv2.BoundingRect(mascara) & new Rect(0, 0, patron.Width, patron.Height);
            using var patronZona = patron[zona].Clone();
            using var muestraZona = colocada[zona].Clone();
            using var mascaraZona = mascara[zona].Clone();

            // Los bordes de la pieza (2,5 mm) no se comparan: cortes, brillos y dobleces
            var interior = new Mat();
            using (var kBorde = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(59, 59)))
            {
                Cv2.Erode(mascaraZona, interior, kBorde);
            }

            using var grisPatron = Luminosidad(patronZona);
            using var grisMuestraBruta = Luminosidad(muestraZona);

            // Ajuste fino por zonas: corrige la curvatura y los pequeños estiramientos de la pieza
            var (muestraColor, grisMuestraAjustada) = AjusteFino(grisPatron, grisMuestraBruta, muestraZona, interior);

            // El tono de cada tinta de la muestra se iguala al del patrón (no se analiza el color)
            var grisMuestra = IgualarTonoPorTinta(patronZona, grisPatron, grisMuestraAjustada, interior);
            grisMuestraAjustada.Dispose();

            var diferencias = BuscarDiferencias(grisPatron, grisMuestra, interior, ajustes)
                .Select(d => d with { Zona = new Rect(d.Zona.X + zona.X, d.Zona.Y + zona.Y, d.Zona.Width, d.Zona.Height) })
                .ToList();

            return new MuestraComparada
            {
                ZonaTrabajo = zona,
                MuestraColor = muestraColor,
                MuestraGris = grisMuestra,
                Interior = interior,
                Diferencias = diferencias,
                Similitud = Similitud(grisPatron, grisMuestra, interior, interior)
            };
        }
    }

    /// <summary>
    /// Luminosidad de una imagen en color (canal L de Lab): cuánta luz tiene cada punto, sin el tono.
    /// </summary>
    private static Mat Luminosidad(Mat color)
    {
        using var lab = color.CvtColor(ColorConversionCodes.BGR2Lab);
        return lab.ExtractChannel(0);
    }

    /// <summary>
    /// Corrige desajustes pequeños por zonas de ~1,4 cm: en cada zona busca cuánto está corrida
    /// la muestra respecto al patrón (como mucho 1 mm) y la endereza, suavizando entre zonas.
    /// Devuelve la muestra en color y en luminosidad ya corregidas.
    /// </summary>
    private static (Mat Color, Mat Gris) AjusteFino(Mat grisPatron, Mat grisMuestra, Mat muestraColor, Mat interior)
    {
        const int Lado_wys = 160;            // tamaño de cada zona (≈ 13,5 mm)
        const int Paso_wys = 80;             // las zonas se solapan a la mitad
        const double DesplazamientoMaximo_wys = 12;   // ≈ 1 mm

        var columnas = Math.Max(1, (grisPatron.Cols - Lado_wys) / Paso_wys + 1);
        var filas = Math.Max(1, (grisPatron.Rows - Lado_wys) / Paso_wys + 1);
        using var dx = new Mat(filas, columnas, MatType.CV_32FC1, Scalar.All(float.NaN));
        using var dy = new Mat(filas, columnas, MatType.CV_32FC1, Scalar.All(float.NaN));

        using var p32 = new Mat();
        using var m32 = new Mat();
        grisPatron.ConvertTo(p32, MatType.CV_32FC1);
        grisMuestra.ConvertTo(m32, MatType.CV_32FC1);
        using var ventana = new Mat();
        Cv2.CreateHanningWindow(ventana, new Size(Lado_wys, Lado_wys), MatType.CV_32FC1);

        for (var f = 0; f < filas; f++)
        {
            for (var c = 0; c < columnas; c++)
            {
                var r = new Rect(c * Paso_wys, f * Paso_wys, Lado_wys, Lado_wys);
                if (r.Right > grisPatron.Cols || r.Bottom > grisPatron.Rows) continue;
                using var cobertura = interior[r];
                if (Cv2.CountNonZero(cobertura) < Lado_wys * Lado_wys * 0.9) continue;
                using var trozoPatron = p32[r];
                Cv2.MeanStdDev(trozoPatron, out _, out var desviacion);
                if (desviacion.Val0 < 8) continue;   // zona lisa: no hay nada con qué comparar

                using var trozoMuestra = m32[r];
                var desplazamiento = Cv2.PhaseCorrelate(trozoPatron, trozoMuestra, ventana, out var confianza);
                if (confianza < 0.1 || Math.Abs(desplazamiento.X) > DesplazamientoMaximo_wys || Math.Abs(desplazamiento.Y) > DesplazamientoMaximo_wys) continue;
                dx.Set(f, c, (float)desplazamiento.X);
                dy.Set(f, c, (float)desplazamiento.Y);
            }
        }

        RellenarHuecos(dx);
        RellenarHuecos(dy);

        // De la rejilla de zonas a un desplazamiento suave para cada píxel
        using var dxGrande = new Mat();
        using var dyGrande = new Mat();
        Cv2.Resize(dx, dxGrande, grisPatron.Size(), 0, 0, InterpolationFlags.Linear);
        Cv2.Resize(dy, dyGrande, grisPatron.Size(), 0, 0, InterpolationFlags.Linear);
        // Mapa de "de dónde coger cada píxel": su propia posición más el desplazamiento de su zona
        int alto = grisPatron.Rows, ancho = grisPatron.Cols;
        using var columnasBase = Mat.FromArray(Enumerable.Range(0, ancho).Select(v => (float)v).ToArray());   // ancho × 1
        using var filasBase = Mat.FromArray(Enumerable.Range(0, alto).Select(v => (float)v).ToArray());       // alto × 1
        using var columnaFila = new Mat();                                                                      // 1 × ancho
        Cv2.Transpose(columnasBase, columnaFila);
        using var baseX = new Mat();
        using var baseY = new Mat();
        Cv2.Repeat(columnaFila, alto, 1, baseX);
        Cv2.Repeat(filasBase, 1, ancho, baseY);
        using var mapaX = new Mat();
        using var mapaY = new Mat();
        Cv2.Add(baseX, dxGrande, mapaX);
        Cv2.Add(baseY, dyGrande, mapaY);

        var color = new Mat();
        var gris = new Mat();
        Cv2.Remap(muestraColor, color, mapaX, mapaY, InterpolationFlags.Linear, BorderTypes.Replicate);
        Cv2.Remap(grisMuestra, gris, mapaX, mapaY, InterpolationFlags.Linear, BorderTypes.Replicate);
        return (color, gris);
    }

    /// <summary>
    /// Las zonas sin dato (lisas o sin muestra) toman el valor medio de las demás, y la rejilla se
    /// suaviza para que no haya saltos entre zonas.
    /// </summary>
    private static void RellenarHuecos(Mat rejilla)
    {
        // La rejilla es pequeña (una casilla por zona de ~7 mm), así que se recorre casilla a casilla
        int filas = rejilla.Rows, columnas = rejilla.Cols;
        var valores = new List<float>();
        for (var f = 0; f < filas; f++)
            for (var c = 0; c < columnas; c++)
            {
                var v = rejilla.At<float>(f, c);
                if (!float.IsNaN(v)) valores.Add(v);
            }
        valores.Sort();
        var mediana = valores.Count > 0 ? valores[valores.Count / 2] : 0f;
        for (var f = 0; f < filas; f++)
            for (var c = 0; c < columnas; c++)
                if (float.IsNaN(rejilla.At<float>(f, c))) rejilla.Set(f, c, mediana);
        if (filas >= 3 && columnas >= 3)
        {
            Cv2.GaussianBlur(rejilla, rejilla, new Size(3, 3), 0);
        }
    }

    /// <summary>
    /// Iguala el tono de cada tinta de la muestra al del patrón. El patrón usa pocas tintas planas:
    /// se agrupan sus colores en "tintas" y, para cada una, se calcula cuánto más clara u oscura sale
    /// en la muestra (la mediana, que no se deja engañar por las manchas) y se corrige. Así un negro
    /// escaneado o un rojo algo más intenso no cuentan como diferencia: no se analiza el color.
    /// </summary>
    private static Mat IgualarTonoPorTinta(Mat patronColor, Mat grisPatron, Mat grisMuestra, Mat interior)
    {
        const int Tintas_wys = 8;

        // 1. Las "tintas" del patrón: grupos de colores parecidos (k-medias sobre una muestra de píxeles)
        using var lab = patronColor.CvtColor(ColorConversionCodes.BGR2Lab);
        using var labPequeno = new Mat();
        Cv2.Resize(lab, labPequeno, new Size(), 0.25, 0.25, InterpolationFlags.Area);
        using var datos = labPequeno.Reshape(1, labPequeno.Rows * labPequeno.Cols);
        using var datos32 = new Mat();
        datos.ConvertTo(datos32, MatType.CV_32FC1);
        using var grupos = new Mat();
        using var centros = new Mat();
        Cv2.Kmeans(datos32, Tintas_wys, grupos, new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 20, 1.0), 2, KMeansFlags.PpCenters, centros);

        // 2. Cada píxel del patrón, a la tinta más parecida
        using var tinta = new Mat(lab.Size(), MatType.CV_8UC1, Scalar.All(0));
        using var mejor = new Mat(lab.Size(), MatType.CV_32FC1, Scalar.All(float.MaxValue));
        using var lab32 = new Mat();
        lab.ConvertTo(lab32, MatType.CV_32FC3);
        for (var t = 0; t < Tintas_wys; t++)
        {
            var centro = new Scalar(centros.At<float>(t, 0), centros.At<float>(t, 1), centros.At<float>(t, 2));
            // Distancia de color al centro de la tinta: suma de los cuadrados de las 3 diferencias
            using var resta = new Mat();
            Cv2.Subtract(lab32, centro, resta);
            using var cuadrado = new Mat();
            Cv2.Multiply(resta, resta, cuadrado);
            var canales = Cv2.Split(cuadrado);
            using var distancia = new Mat();
            Cv2.Add(canales[0], canales[1], distancia);
            Cv2.Add(distancia, canales[2], distancia);
            foreach (var c in canales) c.Dispose();
            using var masCerca = new Mat();
            Cv2.Compare(distancia, mejor, masCerca, CmpTypes.LT);
            distancia.CopyTo(mejor, masCerca);
            tinta.SetTo(new Scalar(t), masCerca);
        }

        // 3. Para cada tinta, la diferencia típica muestra - patrón (mediana) y su corrección
        using var diferencia = new Mat();
        using var m16 = new Mat();
        using var p16 = new Mat();
        grisMuestra.ConvertTo(m16, MatType.CV_16SC1);
        grisPatron.ConvertTo(p16, MatType.CV_16SC1);
        Cv2.Subtract(m16, p16, diferencia);
        using var correccion = new Mat(grisMuestra.Size(), MatType.CV_16SC1, Scalar.All(0));
        for (var t = 0; t < Tintas_wys; t++)
        {
            using var deEstaTinta = new Mat();
            Cv2.Compare(tinta, new Scalar(t), deEstaTinta, CmpTypes.EQ);
            Cv2.BitwiseAnd(deEstaTinta, interior, deEstaTinta);
            if (Cv2.CountNonZero(deEstaTinta) < 500) continue;
            var mediana = MedianaConMascara(diferencia, deEstaTinta);
            correccion.SetTo(new Scalar(mediana), deEstaTinta);
        }

        using var corregida = new Mat();
        Cv2.Subtract(m16, correccion, corregida);
        var resultado = new Mat();
        corregida.ConvertTo(resultado, MatType.CV_8UC1);
        return resultado;
    }

    /// <summary>
    /// Mediana de los valores (de -255 a 255) de una imagen en la zona marcada.
    /// </summary>
    private static int MedianaConMascara(Mat valores16, Mat mascara)
    {
        using var desplazados = new Mat();
        valores16.ConvertTo(desplazados, MatType.CV_16UC1, 1, 256);   // de 1 a 511, para el histograma
        using var histograma = new Mat();
        Cv2.CalcHist([desplazados], [0], mascara, histograma, 1, [512], [new Rangef(0, 512)]);
        var mitad = Cv2.CountNonZero(mascara) / 2.0;
        double acumulado = 0;
        for (var i = 0; i < 512; i++)
        {
            acumulado += histograma.At<float>(i);
            if (acumulado >= mitad) return i - 256;
        }
        return 0;
    }

    /// <summary>
    /// Busca lo que no coincide, con un margen de desajuste:
    /// - "de más": la muestra es más oscura que todo lo que hay alrededor en el patrón (manchas,
    ///   texto doble);
    /// - "de menos": el patrón es más oscuro que todo lo que hay alrededor en la muestra, o la
    ///   muestra es más clara que todo su alrededor en el patrón (falta elemento o tinta).
    /// Se descartan los pliegues, los bordes de la pieza y las guías de montaje del patrón, y lo que es
    /// demasiado pequeño para la tolerancia elegida.
    /// </summary>
    private static List<DiferenciaEnMuestra> BuscarDiferencias(Mat grisPatron, Mat grisMuestra, Mat interior, AjustesTolerancia ajustes)
    {
        var lado = 2 * ajustes.DesajustePx + 1;
        using var kMargen = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(lado, lado));
        using var minPatron = new Mat();
        using var maxPatron = new Mat();
        using var minMuestra = new Mat();
        Cv2.Erode(grisPatron, minPatron, kMargen);
        Cv2.Dilate(grisPatron, maxPatron, kMargen);
        Cv2.Erode(grisMuestra, minMuestra, kMargen);

        using var deMas = new Mat();
        using var masClara = new Mat();
        using var faltaPatron = new Mat();
        Cv2.Subtract(minPatron, grisMuestra, deMas);         // muestra más oscura que su alrededor en el patrón
        Cv2.Subtract(grisMuestra, maxPatron, masClara);      // muestra más clara que su alrededor en el patrón
        Cv2.Subtract(minMuestra, grisPatron, faltaPatron);   // patrón más oscuro que su alrededor en la muestra
        using var deMenos = new Mat();
        Cv2.Max(masClara, faltaPatron, deMenos);

        // Las guías de montaje y marcos del patrón no se imprimen: no cuentan como "falta"
        using (var guias = LineasDeGuia(grisPatron))
        {
            deMenos.SetTo(Scalar.All(0), guias);
        }
        Cv2.GaussianBlur(deMas, deMas, new Size(3, 3), 0);
        Cv2.GaussianBlur(deMenos, deMenos, new Size(3, 3), 0);

        // Umbral: el mínimo de la tolerancia, o más si la muestra tiene mucho "ruido" de escaneo
        var umbral = Math.Max(ajustes.ContrasteMinimo, 4 * RuidoEscaneo(grisPatron, grisMuestra, interior));

        using var binDeMas = Binarizar(deMas, umbral, interior);
        using var binDeMenos = Binarizar(deMenos, umbral, interior);
        QuitarPlieguesYBordes(binDeMas, interior);
        QuitarPlieguesYBordes(binDeMenos, interior);

        // Bordes del patrón (textos, dibujos): sirven para distinguir texto doble de mancha
        using var bordesPatron = new Mat();
        Cv2.Canny(grisPatron, bordesPatron, 50, 150);
        using (var kCerca = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(13, 13)))
        {
            Cv2.Dilate(bordesPatron, bordesPatron, kCerca);
        }

        // Se agrupan los puntos cercanos (a menos de ~1 mm) en una sola diferencia
        using var todo = new Mat();
        Cv2.BitwiseOr(binDeMas, binDeMenos, todo);
        using var agrupado = new Mat();
        using (var kGrupo = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(25, 25)))
        {
            Cv2.Dilate(todo, agrupado, kGrupo);
        }

        using var etiquetas = new Mat();
        using var estadisticas = new Mat();
        using var centros = new Mat();
        var cantidad = Cv2.ConnectedComponentsWithStats(agrupado, etiquetas, estadisticas, centros);
        var resultado = new List<DiferenciaEnMuestra>();
        var areaPixel = 1 / (PixelesPorMm_wys * PixelesPorMm_wys);

        for (var i = 1; i < cantidad; i++)
        {
            var r = new Rect(estadisticas.At<int>(i, 0), estadisticas.At<int>(i, 1), estadisticas.At<int>(i, 2), estadisticas.At<int>(i, 3));
            using var esteGrupo = new Mat();
            using (var etiquetasZona = etiquetas[r])
            {
                Cv2.Compare(etiquetasZona, new Scalar(i), esteGrupo, CmpTypes.EQ);
            }
            using var masZona = binDeMas[r];
            using var menosZona = binDeMenos[r];
            using var masAqui = new Mat();
            using var menosAqui = new Mat();
            Cv2.BitwiseAnd(masZona, esteGrupo, masAqui);
            Cv2.BitwiseAnd(menosZona, esteGrupo, menosAqui);
            var pixelesMas = Cv2.CountNonZero(masAqui);
            var pixelesMenos = Cv2.CountNonZero(menosAqui);
            var areaMm2 = (pixelesMas + pixelesMenos) * areaPixel;
            if (areaMm2 < ajustes.AreaMinimaMm2) continue;

            // Contraste medio de la diferencia (para ordenarlas por evidencia)
            using var deMasZona = deMas[r];
            using var deMenosZona = deMenos[r];
            var contraste = (pixelesMas > 0 ? Cv2.Mean(deMasZona, masAqui).Val0 * pixelesMas : 0)
                          + (pixelesMenos > 0 ? Cv2.Mean(deMenosZona, menosAqui).Val0 * pixelesMenos : 0);
            contraste /= Math.Max(1, pixelesMas + pixelesMenos);

            // Motivo: si hay de más y de menos pegados a un texto, o de más pegado a un texto,
            // es un desplazamiento o texto doble; si no, mancha (de más) o falta (de menos)
            using var bordesZona = bordesPatron[r];
            using var masJuntoATexto = new Mat();
            Cv2.BitwiseAnd(masAqui, bordesZona, masJuntoATexto);
            var proporcionJuntoATexto = pixelesMas > 0 ? (double)Cv2.CountNonZero(masJuntoATexto) / pixelesMas : 0;
            var total = (double)(pixelesMas + pixelesMenos);

            MotivoDiferencia motivo;
            if (pixelesMas / total >= 0.25 && pixelesMenos / total >= 0.25 && proporcionJuntoATexto > 0.5)
                motivo = MotivoDiferencia.Desplazamiento;
            else if (pixelesMas >= pixelesMenos)
                motivo = proporcionJuntoATexto > 0.8 ? MotivoDiferencia.Desplazamiento : MotivoDiferencia.Mancha;
            else
                motivo = MotivoDiferencia.Falta;

            resultado.Add(new DiferenciaEnMuestra(r, motivo, areaMm2, contraste));
        }
        return resultado;
    }

    /// <summary>
    /// Busca en el patrón las líneas finas, rectas y largas (más de 12 mm): guías de montaje,
    /// cotas y marcos de la hoja de aprobación, que no se imprimen en el tubo. Las líneas
    /// discontinuas también cuentan, porque se unen los trozos separados por huecos pequeños.
    /// Devuelve una máscara con esas líneas algo engordadas.
    /// </summary>
    private static Mat LineasDeGuia(Mat grisPatron)
    {
        // Rasgos oscuros más finos que ~0,6 mm ("sombrero negro": lo que se ve oscuro en poco espacio)
        using var finos = new Mat();
        using (var k = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(7, 7)))
        {
            Cv2.MorphologyEx(grisPatron, finos, MorphTypes.BlackHat, k);
        }
        Cv2.Threshold(finos, finos, 25, 255, ThresholdTypes.Binary);

        var mascara = new Mat(grisPatron.Size(), MatType.CV_8UC1, Scalar.Black);
        var lineas = Cv2.HoughLinesP(finos, 1, Math.PI / 180, 80, minLineLength: 12 * PixelesPorMm_wys, maxLineGap: 30);
        foreach (var linea in lineas)
        {
            Cv2.Line(mascara, linea.P1, linea.P2, Scalar.White, 11);
        }
        return mascara;
    }

    /// <summary>
    /// "Ruido" normal del escaneo: cuánto se separan típicamente muestra y patrón en la zona útil
    /// (mediana de las diferencias, robusta frente a los defectos de verdad).
    /// </summary>
    private static double RuidoEscaneo(Mat grisPatron, Mat grisMuestra, Mat interior)
    {
        using var diferencia = new Mat();
        Cv2.Absdiff(grisPatron, grisMuestra, diferencia);
        using var histograma = new Mat();
        Cv2.CalcHist([diferencia], [0], interior, histograma, 1, [256], [new Rangef(0, 256)]);
        var total = Cv2.CountNonZero(interior) / 2.0;
        double acumulado = 0;
        for (var i = 0; i < 256; i++)
        {
            acumulado += histograma.At<float>(i);
            if (acumulado >= total) return i;
        }
        return 0;
    }

    /// <summary>
    /// Deja en blanco los puntos que superan el umbral, solo dentro de la zona útil.
    /// </summary>
    private static Mat Binarizar(Mat mapa, double umbral, Mat interior)
    {
        var binario = new Mat();
        Cv2.Threshold(mapa, binario, umbral, 255, ThresholdTypes.Binary);
        Cv2.BitwiseAnd(binario, interior, binario);
        return binario;
    }

    /// <summary>
    /// Borra los pliegues y bordes, que no se tienen en cuenta (decisión de Alberto, 22/10/2026):
    /// - líneas finas (menos de 0,5 mm de grueso) y largas (más de 4 mm), rectas o curvas;
    /// - cualquier cosa que toque el límite de la zona útil (bordes de la pieza).
    /// Las "sombras" de un texto doble son trozos finos pero cortos, así que se conservan.
    /// </summary>
    private static void QuitarPlieguesYBordes(Mat binario, Mat interior)
    {
        const double GrosorMaximoPx_wys = 6;      // ≈ 0,5 mm
        const int LargoMinimoPx_wys = 47;         // ≈ 4 mm

        // Franja del límite de la zona útil: lo que la toque es borde de la pieza
        using var interiorEstrecho = new Mat();
        using (var k = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(25, 25)))
        {
            Cv2.Erode(interior, interiorEstrecho, k);
        }
        using var franjaBorde = new Mat();
        Cv2.Subtract(interior, interiorEstrecho, franjaBorde);

        // Grosor de cada mancha: el doble de la distancia del punto más interior a su borde
        using var distancia = new Mat();
        Cv2.DistanceTransform(binario, distancia, DistanceTypes.L2, DistanceTransformMasks.Mask5);

        using var etiquetas = new Mat();
        using var estadisticas = new Mat();
        using var centros = new Mat();
        var cantidad = Cv2.ConnectedComponentsWithStats(binario, etiquetas, estadisticas, centros);
        for (var i = 1; i < cantidad; i++)
        {
            var r = new Rect(estadisticas.At<int>(i, 0), estadisticas.At<int>(i, 1), estadisticas.At<int>(i, 2), estadisticas.At<int>(i, 3));
            using var etiquetasZona = etiquetas[r];
            using var esta = new Mat();
            Cv2.Compare(etiquetasZona, new Scalar(i), esta, CmpTypes.EQ);

            using var franjaZona = franjaBorde[r];
            using var tocaBorde = new Mat();
            Cv2.BitwiseAnd(esta, franjaZona, tocaBorde);
            var esBorde = Cv2.CountNonZero(tocaBorde) > 0;

            using var distanciaZona = distancia[r];
            Cv2.MinMaxLoc(distanciaZona, out _, out var maximo, out _, out _, esta);
            var esPliegue = 2 * maximo <= GrosorMaximoPx_wys && Math.Max(r.Width, r.Height) >= LargoMinimoPx_wys;

            if (esBorde || esPliegue)
            {
                using var binarioZona = binario[r];
                binarioZona.SetTo(Scalar.All(0), esta);
            }
        }
    }

    /// <summary>
    /// Parecido entre las dos muestras. Cada una se comparó en su propia zona de la página, así
    /// que primero se colocan las dos en una zona común que las abarca a ambas.
    /// </summary>
    private static double SimilitudEntreMuestras(MuestraComparada m1, MuestraComparada m2)
    {
        var comun = m1.ZonaTrabajo | m2.ZonaTrabajo;
        Mat EnZonaComun(Mat imagen, Rect zona)
        {
            var lienzo = new Mat(comun.Size, imagen.Type(), Scalar.Black);
            using var destino = lienzo[new Rect(zona.X - comun.X, zona.Y - comun.Y, zona.Width, zona.Height)];
            imagen.CopyTo(destino);
            return lienzo;
        }

        using var gris1 = EnZonaComun(m1.MuestraGris, m1.ZonaTrabajo);
        using var gris2 = EnZonaComun(m2.MuestraGris, m2.ZonaTrabajo);
        using var zona1 = EnZonaComun(m1.Interior, m1.ZonaTrabajo);
        using var zona2 = EnZonaComun(m2.Interior, m2.ZonaTrabajo);
        return Similitud(gris1, gris2, zona1, zona2);
    }

    /// <summary>
    /// Parecido entre dos imágenes (índice SSIM, de 0 a 100 %) en la zona común. Solo orientativo.
    /// </summary>
    private static double Similitud(Mat a, Mat b, Mat zonaA, Mat zonaB)
    {
        using var zona = new Mat();
        Cv2.BitwiseAnd(zonaA, zonaB, zona);
        if (Cv2.CountNonZero(zona) == 0) return 0;

        // Se calcula a mitad de resolución: el resultado es casi igual y mucho más rápido
        using var x = new Mat();
        using var y = new Mat();
        using var z = new Mat();
        Cv2.Resize(a, x, new Size(), 0.5, 0.5, InterpolationFlags.Area);
        Cv2.Resize(b, y, new Size(), 0.5, 0.5, InterpolationFlags.Area);
        Cv2.Resize(zona, z, x.Size(), 0, 0, InterpolationFlags.Nearest);
        x.ConvertTo(x, MatType.CV_32FC1);
        y.ConvertTo(y, MatType.CV_32FC1);

        // Fórmula estándar de SSIM: compara brillo medio, contraste y estructura en ventanas pequeñas
        const double C1 = 6.5025, C2 = 58.5225;   // constantes estándar de SSIM para 0-255
        var tamano = new Size(11, 11);
        Mat Suavizar(Mat m) { var r = new Mat(); Cv2.GaussianBlur(m, r, tamano, 1.5); return r; }
        Mat Por(Mat a1, Mat b1) { var r = new Mat(); Cv2.Multiply(a1, b1, r); return r; }

        using var mx = Suavizar(x);
        using var my = Suavizar(y);
        using var xx = Por(x, x);
        using var yy = Por(y, y);
        using var xy = Por(x, y);
        using var sxx = Suavizar(xx);
        using var syy = Suavizar(yy);
        using var sxy = Suavizar(xy);
        using var mxmy = Por(mx, my);
        using var mx2 = Por(mx, mx);
        using var my2 = Por(my, my);

        using var varX = new Mat(); Cv2.Subtract(sxx, mx2, varX);
        using var varY = new Mat(); Cv2.Subtract(syy, my2, varY);
        using var cov = new Mat(); Cv2.Subtract(sxy, mxmy, cov);

        // numerador = (2·mx·my + C1) · (2·cov + C2)
        using var n1 = new Mat(); Cv2.AddWeighted(mxmy, 2, mxmy, 0, C1, n1);
        using var n2 = new Mat(); Cv2.AddWeighted(cov, 2, cov, 0, C2, n2);
        using var numerador = Por(n1, n2);
        // denominador = (mx² + my² + C1) · (varX + varY + C2)
        using var d1 = new Mat(); Cv2.AddWeighted(mx2, 1, my2, 1, C1, d1);
        using var d2 = new Mat(); Cv2.AddWeighted(varX, 1, varY, 1, C2, d2);
        using var denominador = Por(d1, d2);

        using var mapa = new Mat();
        Cv2.Divide(numerador, denominador, mapa);
        return Math.Clamp(Cv2.Mean(mapa, z).Val0 * 100, 0, 100);
    }

    // ------------------------------------------------------------------------------------------
    // Juntar las dos muestras y preparar lo que se ve en pantalla
    // ------------------------------------------------------------------------------------------

    /// <summary>
    /// Diferencia final con su zona en píxeles del patrón, para dibujarla en la vista general.
    /// </summary>
    private sealed record DiferenciaFinal(DiferenciaDetectada Diferencia, Rect Zona);

    /// <summary>
    /// Junta las diferencias de las dos muestras: si salen en el mismo sitio en las dos, son una
    /// sola diferencia "en las dos muestras" (posible fallo de plancha). Las ordena de más a menos
    /// evidente y prepara el recorte de cada una.
    /// </summary>
    private static List<DiferenciaFinal> JuntarDiferencias(Mat patron, MuestraComparada? m1, MuestraComparada? m2)
    {
        var candidatas = new List<(Rect Zona, MotivoDiferencia Motivo, bool En1, bool En2, double Area, double Contraste)>();
        var usadas2 = new HashSet<int>();
        var margen = (int)PixelesPorMm_wys;   // 1 mm de margen para considerar que es "el mismo sitio"

        foreach (var d1 in m1?.Diferencias ?? [])
        {
            var ampliada = new Rect(d1.Zona.X - margen, d1.Zona.Y - margen, d1.Zona.Width + 2 * margen, d1.Zona.Height + 2 * margen);
            var pareja = m2?.Diferencias.Select((d, i) => (d, i)).FirstOrDefault(x => !usadas2.Contains(x.i) && (x.d.Zona & ampliada).Width > 0);
            if (pareja is { } p && p.d is not null)
            {
                usadas2.Add(p.i);
                var motivo = d1.AreaMm2 * d1.Contraste >= p.d.AreaMm2 * p.d.Contraste ? d1.Motivo : p.d.Motivo;
                candidatas.Add((d1.Zona | p.d.Zona, motivo, true, true, Math.Max(d1.AreaMm2, p.d.AreaMm2), Math.Max(d1.Contraste, p.d.Contraste)));
            }
            else
            {
                candidatas.Add((d1.Zona, d1.Motivo, true, false, d1.AreaMm2, d1.Contraste));
            }
        }
        for (var i = 0; i < (m2?.Diferencias.Count ?? 0); i++)
        {
            if (usadas2.Contains(i)) continue;
            var d2 = m2!.Diferencias[i];
            candidatas.Add((d2.Zona, d2.Motivo, false, true, d2.AreaMm2, d2.Contraste));
        }

        // Evidencia: tamaño × contraste, y vale más si sale en las dos muestras
        var ordenadas = candidatas
            .Select(c => (c, Evidencia: c.Area * c.Contraste / 255.0 * (c.En1 && c.En2 ? 1.5 : 1.0)))
            .OrderByDescending(x => x.Evidencia)
            .ToList();

        var resultado = new List<DiferenciaFinal>();
        for (var n = 0; n < ordenadas.Count; n++)
        {
            var (c, evidencia) = ordenadas[n];
            var recorte = Recorte(patron, c.Zona, c.En1 ? m1 : null, c.En2 ? m2 : null);
            var diferencia = new DiferenciaDetectada(n + 1, c.Motivo, c.En1, c.En2,
                c.Zona.X / PixelesPorMm_wys, c.Zona.Y / PixelesPorMm_wys,
                c.Zona.Width / PixelesPorMm_wys, c.Zona.Height / PixelesPorMm_wys,
                Math.Round(c.Area, 2), Math.Round(evidencia, 2), recorte);
            resultado.Add(new DiferenciaFinal(diferencia, c.Zona));
        }
        return resultado;
    }

    /// <summary>
    /// Recorte pequeño en color de una diferencia: el patrón a la izquierda y la(s) muestra(s) a la
    /// derecha, con la zona marcada. Es la evidencia que se guardará en el registro (paso 6).
    /// </summary>
    private static byte[] Recorte(Mat patron, Rect zona, MuestraComparada? m1, MuestraComparada? m2)
    {
        // Al menos 10 mm de lado y 3 mm de margen alrededor de la diferencia
        var minimo = (int)(10 * PixelesPorMm_wys);
        var margen = (int)(3 * PixelesPorMm_wys);
        var ancho = Math.Max(minimo, zona.Width + 2 * margen);
        var alto = Math.Max(minimo, zona.Height + 2 * margen);
        var marco = new Rect(zona.X + zona.Width / 2 - ancho / 2, zona.Y + zona.Height / 2 - alto / 2, ancho, alto)
                    & new Rect(0, 0, patron.Width, patron.Height);

        var piezas = new List<Mat> { patron[marco].Clone() };
        foreach (var m in new[] { m1, m2 })
        {
            if (m is null) continue;
            var pieza = new Mat(marco.Size, MatType.CV_8UC3, Scalar.White);
            var comun = marco & m.ZonaTrabajo;
            if (comun.Width > 0 && comun.Height > 0)
            {
                using var origen = m.MuestraColor[new Rect(comun.X - m.ZonaTrabajo.X, comun.Y - m.ZonaTrabajo.Y, comun.Width, comun.Height)];
                using var destino = pieza[new Rect(comun.X - marco.X, comun.Y - marco.Y, comun.Width, comun.Height)];
                origen.CopyTo(destino);
            }
            piezas.Add(pieza);
        }

        // Rectángulo rojo WYS alrededor de la zona, en cada pieza
        var rectEnMarco = new Rect(zona.X - marco.X, zona.Y - marco.Y, zona.Width, zona.Height);
        foreach (var p in piezas)
        {
            Cv2.Rectangle(p, rectEnMarco, new Scalar(26, 35, 226), 3);
        }

        // Una franja blanca separa las piezas
        using var separador = new Mat(marco.Height, 12, MatType.CV_8UC3, Scalar.White);
        var conSeparadores = new List<Mat>();
        for (var i = 0; i < piezas.Count; i++)
        {
            if (i > 0) conSeparadores.Add(separador);
            conSeparadores.Add(piezas[i]);
        }
        using var unido = new Mat();
        Cv2.HConcat(conSeparadores.ToArray(), unido);
        piezas.ForEach(p => p.Dispose());

        // Pequeño: unos 220 píxeles de alto
        var escala = 220.0 / unido.Rows;
        using var pequeno = new Mat();
        Cv2.Resize(unido, pequeno, new Size(Math.Max(1, (int)(unido.Cols * escala)), 220), 0, 0, InterpolationFlags.Area);
        Cv2.ImEncode(".jpg", pequeno, out var jpeg, new ImageEncodingParam(ImwriteFlags.JpegQuality, 85));
        return jpeg;
    }

    /// <summary>
    /// Color de cada motivo en la vista general (en BGR): rojo WYS para manchas, azul WYS para
    /// faltas y naranja para desplazamientos.
    /// </summary>
    public static Scalar ColorMotivo(MotivoDiferencia motivo) => motivo switch
    {
        MotivoDiferencia.Mancha => new Scalar(26, 35, 226),
        MotivoDiferencia.Falta => new Scalar(145, 74, 37),
        _ => new Scalar(0, 140, 255)
    };

    /// <summary>
    /// Vista general: la zona del tubo en el patrón con cada diferencia enmarcada y numerada,
    /// con el color de su motivo.
    /// </summary>
    private static byte[] VistaGeneral(Mat patron, List<DiferenciaFinal> diferencias, MuestraComparada? m1, MuestraComparada? m2)
    {
        using var vista = patron.Clone();
        var zona = new Rect();
        foreach (var m in new[] { m1, m2 })
        {
            if (m is null) continue;
            zona = zona.Width == 0 ? m.ZonaTrabajo : zona | m.ZonaTrabajo;
        }

        foreach (var d in diferencias)
        {
            var color = ColorMotivo(d.Diferencia.Motivo);
            var r = new Rect(d.Zona.X - 8, d.Zona.Y - 8, d.Zona.Width + 16, d.Zona.Height + 16);
            Cv2.Rectangle(vista, r, color, 5);
            var texto = d.Diferencia.Numero.ToString();
            var punto = new Point(r.X, Math.Max(30, r.Y - 8));
            Cv2.PutText(vista, texto, punto, HersheyFonts.HersheySimplex, 1.4, Scalar.White, 10);
            Cv2.PutText(vista, texto, punto, HersheyFonts.HersheySimplex, 1.4, color, 4);
        }

        zona = new Rect(zona.X - 60, zona.Y - 60, zona.Width + 120, zona.Height + 120) & new Rect(0, 0, patron.Width, patron.Height);
        using var recorte = vista[zona];
        return ServicioArchivosAutocontrol.VistaReducida(recorte, 1100);
    }
}
