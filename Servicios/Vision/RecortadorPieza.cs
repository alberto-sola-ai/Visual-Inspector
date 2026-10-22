using OpenCvSharp;

namespace VisualInspector.Servicios.Vision;

/// <summary>
/// Separa la pieza del tubo del fondo del escáner, como quien recorta una foto de una revista:
/// quita las bandas grises de los bordes y el polvo, y se queda con la mancha grande que es la pieza.
/// </summary>
public static class RecortadorPieza
{
    // Diferencia mínima con el color del fondo para considerar que un píxel es pieza
    private const int UmbralDiferenciaFondo_wys = 30;
    // Una fila o columna del borde es "banda gris" si es casi uniforme y más oscura que el fondo
    private const double DesviacionMaximaBanda_wys = 14;
    private const double OscuridadMinimaBanda_wys = 12;
    private const int MargenBanda_wys = 8;
    // Tamaños (en píxeles a 300 ppp) para quitar polvo y unir el texto en una sola mancha
    private const int TamanoPolvo_wys = 7;
    private const int TamanoUnion_wys = 101;

    /// <summary>
    /// Devuelve la pieza recortada (en color) y su máscara: blanco = pieza, negro = fondo.
    /// La imagen original no se modifica.
    /// </summary>
    public static (Mat Pieza, Mat Mascara) Recortar(Mat muestra)
    {
        using var copia = muestra.Clone();

        // 1. Color del fondo del escáner: la mediana de cada canal (el fondo ocupa la mayor parte)
        var fondo = ColorFondo(copia);

        // 2. Las bandas grises de los bordes (tapa del escáner) se pintan del color del fondo
        QuitarBandas(copia, fondo);

        // 3. Píxeles que se apartan claramente del color del fondo
        using var mascaraBruta = DistintoDelFondo(copia, fondo);

        // 4. Quitar polvo suelto y unir el texto impreso en una sola mancha
        using var kPolvo = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(TamanoPolvo_wys, TamanoPolvo_wys));
        using var kUnion = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(TamanoUnion_wys, TamanoUnion_wys));
        Cv2.MorphologyEx(mascaraBruta, mascaraBruta, MorphTypes.Open, kPolvo);
        Cv2.MorphologyEx(mascaraBruta, mascaraBruta, MorphTypes.Close, kUnion);

        // 5. La pieza es la mancha más grande; su envolvente recupera las zonas blancas del tubo
        Cv2.FindContours(mascaraBruta, out var contornos, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        if (contornos.Length == 0)
        {
            // No se distingue nada del fondo: se usa la imagen entera y la localización decidirá
            return (muestra.Clone(), new Mat(muestra.Size(), MatType.CV_8UC1, Scalar.White));
        }

        var mayor = contornos.OrderByDescending(c => Cv2.ContourArea(c)).First();
        var envolvente = Cv2.ConvexHull(mayor);
        using var mascara = new Mat(muestra.Size(), MatType.CV_8UC1, Scalar.Black);
        Cv2.FillConvexPoly(mascara, envolvente, Scalar.White);
        var rect = Cv2.BoundingRect(envolvente);
        return (muestra[rect].Clone(), mascara[rect].Clone());
    }

    /// <summary>
    /// Calcula el color del fondo del escáner (mediana de cada canal).
    /// </summary>
    private static Scalar ColorFondo(Mat imagen)
    {
        var canales = Cv2.Split(imagen);
        try
        {
            return new Scalar(Mediana(canales[0]), Mediana(canales[1]), Mediana(canales[2]));
        }
        finally
        {
            foreach (var c in canales)
            {
                c.Dispose();
            }
        }
    }

    /// <summary>
    /// Pinta del color del fondo las bandas grises uniformes que deja la tapa del escáner en los bordes.
    /// </summary>
    private static void QuitarBandas(Mat imagen, Scalar fondo)
    {
        using var gris = imagen.CvtColor(ColorConversionCodes.BGR2GRAY);
        var grisFondo = fondo.Val0 * 0.114 + fondo.Val1 * 0.587 + fondo.Val2 * 0.299;

        bool EsBanda(Mat linea)
        {
            // La fila o columna se suelta en cuanto se ha mirado
            using (linea)
            {
                Cv2.MeanStdDev(linea, out var media, out var desviacion);
                return desviacion.Val0 < DesviacionMaximaBanda_wys && grisFondo - media.Val0 > OscuridadMinimaBanda_wys;
            }
        }

        // Se buscan bandas como mucho en la cuarta parte de cada borde
        int filas = gris.Rows, columnas = gris.Cols;
        int arriba = 0, abajo = 0, izquierda = 0, derecha = 0;
        while (arriba < filas / 4 && EsBanda(gris.Row(arriba))) arriba++;
        while (abajo < filas / 4 && EsBanda(gris.Row(filas - 1 - abajo))) abajo++;
        while (izquierda < columnas / 4 && EsBanda(gris.Col(izquierda))) izquierda++;
        while (derecha < columnas / 4 && EsBanda(gris.Col(columnas - 1 - derecha))) derecha++;

        // Unos píxeles de margen por si el borde de la banda es irregular
        if (arriba > 0) Pintar(imagen, new Rect(0, 0, imagen.Cols, arriba + MargenBanda_wys), fondo);
        if (abajo > 0) Pintar(imagen, new Rect(0, imagen.Rows - abajo - MargenBanda_wys, imagen.Cols, abajo + MargenBanda_wys), fondo);
        if (izquierda > 0) Pintar(imagen, new Rect(0, 0, izquierda + MargenBanda_wys, imagen.Rows), fondo);
        if (derecha > 0) Pintar(imagen, new Rect(imagen.Cols - derecha - MargenBanda_wys, 0, derecha + MargenBanda_wys, imagen.Rows), fondo);
    }

    private static void Pintar(Mat imagen, Rect zona, Scalar color)
    {
        var dentro = zona & new Rect(0, 0, imagen.Cols, imagen.Rows);
        if (dentro.Width > 0 && dentro.Height > 0)
        {
            using var parte = imagen[dentro];
            parte.SetTo(color);
        }
    }

    /// <summary>
    /// Marca en blanco los píxeles cuyo color se aparta del fondo en algún canal.
    /// </summary>
    private static Mat DistintoDelFondo(Mat imagen, Scalar fondo)
    {
        var canales = Cv2.Split(imagen);
        var distancia = new Mat(imagen.Size(), MatType.CV_8UC1, Scalar.Black);
        try
        {
            for (var c = 0; c < 3; c++)
            {
                using var diferencia = new Mat();
                Cv2.Absdiff(canales[c], new Scalar(fondo[c]), diferencia);
                Cv2.Max(distancia, diferencia, distancia);
            }
            Cv2.Threshold(distancia, distancia, UmbralDiferenciaFondo_wys, 255, ThresholdTypes.Binary);
            return distancia;
        }
        finally
        {
            foreach (var c in canales)
            {
                c.Dispose();
            }
        }
    }

    /// <summary>
    /// Valor central de un canal (la mitad de los píxeles está por debajo).
    /// </summary>
    private static byte Mediana(Mat canal)
    {
        using var histograma = new Mat();
        Cv2.CalcHist([canal], [0], null, histograma, 1, [256], [new Rangef(0, 256)]);
        var mitad = canal.Rows * (double)canal.Cols / 2;
        double acumulado = 0;
        for (var i = 0; i < 256; i++)
        {
            acumulado += histograma.At<float>(i);
            if (acumulado >= mitad)
            {
                return (byte)i;
            }
        }
        return 255;
    }
}
