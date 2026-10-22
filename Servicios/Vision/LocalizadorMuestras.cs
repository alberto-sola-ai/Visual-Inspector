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
/// Encuentra dónde está cada muestra escaneada dentro de la página del patrón, como quien busca
/// una pieza en un puzle: mira detalles que coinciden (letras, esquinas, dibujos) y deduce cuánto
/// está girada y dónde va. La muestra puede venir en cualquier orientación y el tubo abierto por
/// cualquier punto. El cajetín, las cotas y los logos de la hoja del patrón no encajan con nada
/// y se descartan solos.
/// </summary>
public sealed class LocalizadorMuestras(ServicioAutorizacion autorizacion)
{
    // Los puntos se buscan a 150 ppp (mitad de resolución): es suficiente y mucho más rápido
    private const double EscalaBusqueda_wys = 0.5;
    private const int MaximoPuntos_wys = 20000;
    private const float UmbralParecido_wys = 0.75f;      // prueba del "segundo mejor" de Lowe
    private const int MinimoCoincidencias_wys = 20;
    private const int MaximoTrozos_wys = 3;
    private const double ToleranciaEscala_wys = 0.1;      // ±10 %: ambos deben estar a 300 ppp
    private const int TiempoMaximoSegundos_wys = 120;
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
        using var patron = ServicioArchivosAutocontrol.DibujarPatron(pdf);
        using var sift = SIFT.Create(MaximoPuntos_wys);

        // Los puntos se buscan sobre una copia en gris y reducida; lo que se muestra sigue en color
        using var patronGris = CopiaGrisReducida(patron);
        using var descPatron = new Mat();
        sift.DetectAndCompute(patronGris, null, out var puntosPatron, descPatron);

        var resultados = new List<ResultadoLocalizacion>();
        foreach (var datos in muestras)
        {
            using var muestra = ServicioArchivosAutocontrol.LeerImagen(datos);
            resultados.Add(LocalizarUna(patron, muestra, sift, puntosPatron, descPatron));
        }
        return resultados;
    }

    /// <summary>
    /// Recorta la pieza de la muestra, busca sus trozos en el patrón y prepara la vista superpuesta.
    /// </summary>
    private static ResultadoLocalizacion LocalizarUna(Mat patron, Mat muestra, SIFT sift, KeyPoint[] puntosPatron, Mat descPatron)
    {
        var (pieza, mascara) = RecortadorPieza.Recortar(muestra);
        using (pieza)
        using (mascara)
        {
            var anchoMm = pieza.Width * 25.4 / ServicioArchivosAutocontrol.Resolucion_wys;
            var altoMm = pieza.Height * 25.4 / ServicioArchivosAutocontrol.Resolucion_wys;

            using var piezaGris = CopiaGrisReducida(pieza);
            using var mascaraReducida = new Mat();
            Cv2.Resize(mascara, mascaraReducida, piezaGris.Size(), 0, 0, InterpolationFlags.Nearest);
            using var descPieza = new Mat();
            sift.DetectAndCompute(piezaGris, mascaraReducida, out var puntosPieza, descPieza);

            if (puntosPieza.Length < MinimoCoincidencias_wys || descPatron.Empty())
            {
                return NoLocalizada(anchoMm, altoMm, "La muestra no tiene suficiente detalle para localizarla. Comprueba que el escaneo es correcto.");
            }

            // Para cada punto de la muestra, el más parecido del patrón; solo se aceptan los que
            // se parecen claramente más al primero que al segundo candidato
            using var comparador = new BFMatcher(NormTypes.L2);
            var buenos = comparador.KnnMatch(descPieza, descPatron, 2)
                .Where(m => m.Length == 2 && m[0].Distance < UmbralParecido_wys * m[1].Distance)
                .Select(m => m[0])
                .ToList();

            var trozos = BuscarTrozos(buenos, puntosPieza, puntosPatron, out var escalaFuera);
            if (trozos.Count == 0)
            {
                var aviso = escalaFuera
                    ? "La muestra aparece en el patrón, pero a otro tamaño: comprueba que está escaneada a 300 ppp."
                    : "No se ha encontrado la muestra en el patrón. Comprueba que el patrón es el correcto.";
                return NoLocalizada(anchoMm, altoMm, aviso);
            }

            var vista = Superponer(patron, pieza, mascara, puntosPieza, trozos);
            return new ResultadoLocalizacion(true,
                trozos.Select(t => new TrozoLocalizado(t.GiroGrados, t.Coincidencias.Count)).ToList(),
                anchoMm, altoMm, vista, null);
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
    private static byte[] Superponer(Mat patron, Mat pieza, Mat mascara, KeyPoint[] puntosPieza, List<Trozo> trozos)
    {
        using var mezcla = patron.Clone();
        var colores = new[] { new Scalar(26, 35, 226), new Scalar(145, 74, 37), new Scalar(0, 160, 0) };   // rojo WYS, azul WYS, verde
        var zonaTotal = new Rect();

        var zonas = ZonasPorTrozo(mascara, puntosPieza, trozos);
        for (var i = 0; i < trozos.Count; i++)
        {
            using var zona = zonas[i];
            using var paso = APasoCompleto(trozos[i].Transformacion);
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
    private static Mat CopiaGrisReducida(Mat imagen)
    {
        using var reducida = new Mat();
        Cv2.Resize(imagen, reducida, new Size(), EscalaBusqueda_wys, EscalaBusqueda_wys, InterpolationFlags.Area);
        return reducida.CvtColor(ColorConversionCodes.BGR2GRAY);
    }

    private static ResultadoLocalizacion NoLocalizada(double anchoMm, double altoMm, string aviso) =>
        new(false, [], anchoMm, altoMm, null, aviso);
}
