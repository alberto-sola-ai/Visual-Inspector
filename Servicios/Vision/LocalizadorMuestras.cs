using OpenCvSharp;
using OpenCvSharp.Features2D;
using VisualInspector.Seguridad;

namespace VisualInspector.Servicios.Vision;

/// <summary>
/// Un trozo de la muestra encontrado en el patrón. Normalmente hay uno; hay dos cuando el tubo
/// se abrió por un punto distinto de la costura y el diseño quedó partido.
/// </summary>
/// <param name="GiroGrados">Cuánto estaba girada la muestra en el escáner.</param>
/// <param name="Coincidencias">Puntos que encajan: cuantos más, más seguro es el resultado.</param>
public sealed record TrozoLocalizado(double GiroGrados, int Coincidencias);

/// <summary>
/// Resultado de localizar una muestra dentro del patrón.
/// </summary>
/// <param name="EsLocalizada">Si se ha encontrado la muestra en el patrón.</param>
/// <param name="Trozos">Trozos encontrados (giro y coincidencias de cada uno).</param>
/// <param name="AnchoPiezaMm">Ancho de la pieza recortada del fondo del escáner.</param>
/// <param name="AltoPiezaMm">Alto de la pieza recortada.</param>
/// <param name="VistaSuperpuesta">Patrón con la muestra encima, en color, para comprobarlo a ojo.</param>
/// <param name="Aviso">Explicación para el usuario si algo no ha ido bien.</param>
public sealed record ResultadoLocalizacion(
    bool EsLocalizada, IReadOnlyList<TrozoLocalizado> Trozos, double AnchoPiezaMm, double AltoPiezaMm,
    byte[]? VistaSuperpuesta, string? Aviso);

/// <summary>
/// El patrón listo para buscar muestras en él: su imagen a 300 ppp y sus puntos característicos.
/// Se prepara una sola vez y sirve para las dos muestras.
/// </summary>
public sealed class PatronPreparado : IDisposable
{
    public Mat Imagen { get; }
    internal SIFT Buscador { get; }
    internal KeyPoint[] Puntos { get; }
    internal Mat Descriptores { get; }

    private PatronPreparado(Mat imagen, SIFT buscador, KeyPoint[] puntos, Mat descriptores)
    {
        Imagen = imagen;
        Buscador = buscador;
        Puntos = puntos;
        Descriptores = descriptores;
    }

    /// <summary>
    /// Dibuja el PDF a 300 ppp y busca sus puntos característicos (letras, esquinas, dibujos).
    /// </summary>
    public static PatronPreparado Crear(byte[] pdf)
    {
        var imagen = ServicioArchivosAutocontrol.DibujarPatron(pdf);
        var buscador = SIFT.Create(LocalizadorMuestras.MaximoPuntos_wys);
        using var gris = LocalizadorMuestras.CopiaGrisReducida(imagen);
        var descriptores = new Mat();
        buscador.DetectAndCompute(gris, null, out var puntos, descriptores);
        return new PatronPreparado(imagen, buscador, puntos, descriptores);
    }

    public void Dispose()
    {
        Imagen.Dispose();
        Buscador.Dispose();
        Descriptores.Dispose();
    }
}

/// <summary>
/// Una muestra ya localizada en el patrón: la pieza recortada y, para cada trozo, cómo girarla y
/// moverla para colocarla encima del patrón. La usa la comparación (paso 5).
/// </summary>
public sealed class MuestraLocalizada : IDisposable
{
    public ResultadoLocalizacion Resultado { get; }
    public Mat Pieza { get; }
    // Para cada trozo: el giro + desplazamiento a 300 ppp y la zona de la pieza que le corresponde
    internal List<(Mat Paso, Mat Zona)> Trozos { get; }

    internal MuestraLocalizada(ResultadoLocalizacion resultado, Mat pieza, List<(Mat Paso, Mat Zona)> trozos)
    {
        Resultado = resultado;
        Pieza = pieza;
        Trozos = trozos;
    }

    /// <summary>
    /// Coloca la muestra encima del patrón (girada y movida, trozo a trozo) y devuelve la imagen
    /// resultante y su máscara: blanco donde hay muestra, negro donde no.
    /// </summary>
    public (Mat Imagen, Mat Mascara) ColocarEnPatron(Size tamanoPatron)
    {
        var imagen = new Mat(tamanoPatron, MatType.CV_8UC3, Scalar.White);
        var mascara = new Mat(tamanoPatron, MatType.CV_8UC1, Scalar.Black);
        foreach (var (paso, zona) in Trozos)
        {
            using var piezaEnPatron = new Mat();
            using var zonaEnPatron = new Mat();
            Cv2.WarpAffine(Pieza, piezaEnPatron, paso, tamanoPatron, InterpolationFlags.Linear);
            Cv2.WarpAffine(zona, zonaEnPatron, paso, tamanoPatron, InterpolationFlags.Nearest);
            piezaEnPatron.CopyTo(imagen, zonaEnPatron);
            Cv2.BitwiseOr(mascara, zonaEnPatron, mascara);
        }
        return (imagen, mascara);
    }

    public void Dispose()
    {
        Pieza.Dispose();
        foreach (var (paso, zona) in Trozos)
        {
            paso.Dispose();
            zona.Dispose();
        }
    }
}

/// <summary>
/// Encuentra dónde está cada muestra escaneada dentro de la página del patrón, como quien busca
/// una pieza en un puzle: mira detalles que coinciden (letras, esquinas, dibujos) y deduce cuánto
/// está girada y dónde va. La muestra puede venir en cualquier orientación y el tubo abierto por
/// cualquier punto. El cajetín, las cotas y los logos de la hoja del patrón no encajan con nada
/// y se descartan solos.
/// </summary>
public sealed class LocalizadorMuestras(ServicioAutorizacion autorizacion)
{
    // Los puntos se buscan a 150 ppp (mitad de resolución): es suficiente y mucho más rápido
    internal const double EscalaBusqueda_wys = 0.5;
    internal const int MaximoPuntos_wys = 20000;
    private const float UmbralParecido_wys = 0.75f;      // prueba del "segundo mejor" de Lowe
    private const int MinimoCoincidencias_wys = 20;
    private const int MaximoTrozos_wys = 3;
    private const double ToleranciaEscala_wys = 0.1;      // ±10 %: ambos deben estar a 300 ppp
    internal const int TiempoMaximoSegundos_wys = 120;
    private const int LadoVista_wys = 1100;

    /// <summary>
    /// Localiza todas las muestras en el patrón. Devuelve un resultado por muestra, en el mismo orden.
    /// </summary>
    public async Task<IReadOnlyList<ResultadoLocalizacion>> LocalizarAsync(ArchivoPreparado patron, IReadOnlyList<ArchivoPreparado> muestras)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);

        // Fuera del hilo de la pantalla y con tiempo máximo, para que un archivo raro no bloquee nada
        return await Task.Run(() => LocalizarTodas(patron.Contenido, muestras.Select(m => m.Contenido).ToList()))
            .WaitAsync(TimeSpan.FromSeconds(TiempoMaximoSegundos_wys));
    }

    /// <summary>
    /// Prepara el patrón una sola vez y busca en él cada muestra.
    /// </summary>
    private static List<ResultadoLocalizacion> LocalizarTodas(byte[] pdf, List<byte[]> muestras)
    {
        // Semilla fija: los mismos archivos dan siempre el mismo resultado
        Cv2.SetTheRNG(20261022);
        using var patron = PatronPreparado.Crear(pdf);
        var resultados = new List<ResultadoLocalizacion>();
        foreach (var datos in muestras)
        {
            using var muestra = ServicioArchivosAutocontrol.LeerImagen(datos);
            using var localizada = Localizar(patron, muestra);
            resultados.Add(localizada.Resultado);
        }
        return resultados;
    }

    /// <summary>
    /// Recorta la pieza de la muestra, busca sus trozos en el patrón y prepara la vista superpuesta.
    /// Devuelve todo lo necesario para comparar después.
    /// </summary>
    public static MuestraLocalizada Localizar(PatronPreparado patron, Mat muestra)
    {
        var (pieza, mascara) = RecortadorPieza.Recortar(muestra);
        using (mascara)
        {
            var anchoMm = pieza.Width * 25.4 / ServicioArchivosAutocontrol.Resolucion_wys;
            var altoMm = pieza.Height * 25.4 / ServicioArchivosAutocontrol.Resolucion_wys;

            using var piezaGris = CopiaGrisReducida(pieza);
            using var mascaraReducida = new Mat();
            Cv2.Resize(mascara, mascaraReducida, piezaGris.Size(), 0, 0, InterpolationFlags.Nearest);
            using var descPieza = new Mat();
            patron.Buscador.DetectAndCompute(piezaGris, mascaraReducida, out var puntosPieza, descPieza);

            if (puntosPieza.Length < MinimoCoincidencias_wys || patron.Descriptores.Empty())
            {
                return NoLocalizada(pieza, anchoMm, altoMm, "La muestra no tiene suficiente detalle para localizarla. Comprueba que el escaneo es correcto.");
            }

            // Para cada punto de la muestra, el más parecido del patrón; solo se aceptan los que
            // se parecen claramente más al primero que al segundo candidato
            using var comparador = new BFMatcher(NormTypes.L2);
            var buenos = comparador.KnnMatch(descPieza, patron.Descriptores, 2)
                .Where(m => m.Length == 2 && m[0].Distance < UmbralParecido_wys * m[1].Distance)
                .Select(m => m[0])
                .ToList();

            var trozos = BuscarTrozos(buenos, puntosPieza, patron.Puntos, out var escalaFuera);
            if (trozos.Count == 0)
            {
                var aviso = escalaFuera
                    ? "La muestra aparece en el patrón, pero a otro tamaño: comprueba que está escaneada a 300 ppp."
                    : "No se ha encontrado la muestra en el patrón. Comprueba que el patrón es el correcto.";
                return NoLocalizada(pieza, anchoMm, altoMm, aviso);
            }

            var zonas = ZonasPorTrozo(mascara, puntosPieza, trozos);
            var pasos = trozos.Select(t => APasoCompleto(t.Transformacion)).ToList();
            var colocados = pasos.Zip(zonas).Select(p => (p.First, p.Second)).ToList();

            var vista = Superponer(patron.Imagen, pieza, colocados);
            var resultado = new ResultadoLocalizacion(true,
                trozos.Select(t => new TrozoLocalizado(t.GiroGrados, t.Coincidencias.Count)).ToList(),
                anchoMm, altoMm, vista, null);
            return new MuestraLocalizada(resultado, pieza, colocados);
        }
    }

    /// <summary>
    /// Un trozo encontrado: su giro y desplazamiento (a 150 ppp) y las coincidencias que lo forman.
    /// </summary>
    private sealed record Trozo(double[] Transformacion, double GiroGrados, List<DMatch> Coincidencias);

    /// <summary>
    /// Busca el giro y el desplazamiento que explican la mayoría de coincidencias (primer trozo).
    /// Con las que sobran se repite la búsqueda: si encajan con otro desplazamiento, es el segundo
    /// trozo del tubo. Solo valen giros sin cambio de tamaño, porque todo está a 300 ppp.
    /// </summary>
    private static List<Trozo> BuscarTrozos(List<DMatch> coincidencias, KeyPoint[] puntosPieza, KeyPoint[] puntosPatron, out bool escalaFuera)
    {
        escalaFuera = false;
        var trozos = new List<Trozo>();
        var restantes = coincidencias;

        for (var intento = 0; intento < MaximoTrozos_wys && restantes.Count >= MinimoCoincidencias_wys; intento++)
        {
            var origen = restantes.Select(m => puntosPieza[m.QueryIdx].Pt).ToArray();
            var destino = restantes.Select(m => puntosPatron[m.TrainIdx].Pt).ToArray();
            using var validos = new Mat();
            // RANSAC: prueba muchas combinaciones al azar y se queda con la que más puntos explica
            using var transformacion = Cv2.EstimateAffinePartial2D(InputArray.Create(origen), InputArray.Create(destino),
                validos, RobustEstimationAlgorithms.RANSAC, 3, 5000, 0.995, 10);
            if (transformacion is null || transformacion.Empty())
            {
                break;
            }

            var t = new double[6];
            for (var i = 0; i < 6; i++)
            {
                t[i] = transformacion.At<double>(i / 3, i % 3);
            }
            var encajan = restantes.Where((_, i) => validos.At<byte>(i) != 0).ToList();
            var escala = Math.Sqrt(t[0] * t[0] + t[3] * t[3]);

            if (encajan.Count < MinimoCoincidencias_wys)
            {
                break;
            }
            if (Math.Abs(escala - 1) > ToleranciaEscala_wys)
            {
                // Solo cuenta como "otro tamaño" si es lo primero que se encuentra
                escalaFuera = trozos.Count == 0;
                break;
            }

            trozos.Add(new Trozo(t, Math.Atan2(t[3], t[0]) * 180 / Math.PI, encajan));
            restantes = restantes.Except(encajan).ToList();
        }
        return trozos;
    }

    /// <summary>
    /// Dibuja el patrón con la muestra encima (mitad y mitad, en color) y el contorno de cada trozo,
    /// recortado a la zona del tubo y reducido para la pantalla.
    /// </summary>
    private static byte[] Superponer(Mat patron, Mat pieza, List<(Mat Paso, Mat Zona)> trozos)
    {
        using var mezcla = patron.Clone();
        var colores = new[] { new Scalar(26, 35, 226), new Scalar(145, 74, 37), new Scalar(0, 160, 0) };   // rojo WYS, azul WYS, verde
        var zonaTotal = new Rect();

        for (var i = 0; i < trozos.Count; i++)
        {
            var (paso, zona) = trozos[i];
            using var piezaEnPatron = new Mat();
            using var zonaEnPatron = new Mat();
            Cv2.WarpAffine(pieza, piezaEnPatron, paso, patron.Size());
            Cv2.WarpAffine(zona, zonaEnPatron, paso, patron.Size(), InterpolationFlags.Nearest);

            using var mitad = new Mat();
            Cv2.AddWeighted(patron, 0.5, piezaEnPatron, 0.5, 0, mitad);
            mitad.CopyTo(mezcla, zonaEnPatron);

            Cv2.FindContours(zonaEnPatron, out var contornos, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
            Cv2.DrawContours(mezcla, contornos, -1, colores[i % colores.Length], 6);
            var r = Cv2.BoundingRect(zonaEnPatron);
            zonaTotal = zonaTotal.Width == 0 ? r : zonaTotal | r;
        }

        // Un margen alrededor para ver el patrón que rodea la muestra
        const int Margen_wys = 120;
        zonaTotal = new Rect(zonaTotal.X - Margen_wys, zonaTotal.Y - Margen_wys, zonaTotal.Width + 2 * Margen_wys, zonaTotal.Height + 2 * Margen_wys)
                    & new Rect(0, 0, patron.Width, patron.Height);
        using var recorte = mezcla[zonaTotal];
        return ServicioArchivosAutocontrol.VistaReducida(recorte, LadoVista_wys);
    }

    /// <summary>
    /// Reparte los píxeles de la pieza entre los trozos: cada píxel va con el trozo cuyas
    /// coincidencias tiene más cerca, así la frontera entre trozos cae en la línea de corte del tubo.
    /// </summary>
    private static List<Mat> ZonasPorTrozo(Mat mascara, KeyPoint[] puntosPieza, List<Trozo> trozos)
    {
        if (trozos.Count == 1)
        {
            return [mascara.Clone()];
        }

        var distancias = trozos.Select(trozo =>
        {
            using var puntos = new Mat(mascara.Size(), MatType.CV_8UC1, Scalar.White);
            foreach (var m in trozo.Coincidencias)
            {
                var x = (int)(puntosPieza[m.QueryIdx].Pt.X / EscalaBusqueda_wys);
                var y = (int)(puntosPieza[m.QueryIdx].Pt.Y / EscalaBusqueda_wys);
                if (x >= 0 && y >= 0 && x < puntos.Cols && y < puntos.Rows)
                {
                    puntos.Set(y, x, (byte)0);
                }
            }
            var distancia = new Mat();
            Cv2.DistanceTransform(puntos, distancia, DistanceTypes.L2, DistanceTransformMasks.Mask5);
            return distancia;
        }).ToList();

        var zonas = new List<Mat>();
        for (var i = 0; i < trozos.Count; i++)
        {
            var zona = mascara.Clone();
            for (var j = 0; j < trozos.Count; j++)
            {
                if (j == i)
                {
                    continue;
                }
                using var masCerca = new Mat();
                Cv2.Compare(distancias[i], distancias[j], masCerca, CmpTypes.LE);
                Cv2.BitwiseAnd(zona, masCerca, zona);
            }
            zonas.Add(zona);
        }
        distancias.ForEach(d => d.Dispose());
        return zonas;
    }

    /// <summary>
    /// Pasa el giro y desplazamiento calculados a 150 ppp a la resolución completa (300 ppp):
    /// el giro es el mismo y el desplazamiento se duplica.
    /// </summary>
    private static Mat APasoCompleto(double[] t)
    {
        var paso = new Mat(2, 3, MatType.CV_64FC1);
        paso.Set(0, 0, t[0]); paso.Set(0, 1, t[1]); paso.Set(0, 2, t[2] / EscalaBusqueda_wys);
        paso.Set(1, 0, t[3]); paso.Set(1, 1, t[4]); paso.Set(1, 2, t[5] / EscalaBusqueda_wys);
        return paso;
    }

    /// <summary>
    /// Copia en gris y a mitad de tamaño, solo para buscar puntos (no se muestra nunca).
    /// </summary>
    internal static Mat CopiaGrisReducida(Mat imagen)
    {
        using var reducida = new Mat();
        Cv2.Resize(imagen, reducida, new Size(), EscalaBusqueda_wys, EscalaBusqueda_wys, InterpolationFlags.Area);
        return reducida.CvtColor(ColorConversionCodes.BGR2GRAY);
    }

    private static MuestraLocalizada NoLocalizada(Mat pieza, double anchoMm, double altoMm, string aviso) =>
        new(new ResultadoLocalizacion(false, [], anchoMm, altoMm, null, aviso), pieza, []);
}
