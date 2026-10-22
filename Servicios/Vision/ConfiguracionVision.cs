using OpenCvSharp;

namespace VisualInspector.Servicios.Vision;

/// <summary>
/// Ajustes de OpenCV (la biblioteca de visión artificial) que se aplican una vez al arrancar.
/// </summary>
public static class ConfiguracionVision
{
    // Tope de píxeles por imagen que OpenCV acepta leer (≈ 12.000 × 12.000): frena imágenes
    // preparadas para agotar la memoria del servidor
    private const string MaximoPixeles_wys = "144000000";

    /// <summary>
    /// Deja OpenCV listo antes de usarlo por primera vez.
    /// </summary>
    public static void Aplicar()
    {
        // Debe ponerse antes de la primera lectura de imagen, porque OpenCV lo lee al empezar
        Environment.SetEnvironmentVariable("OPENCV_IO_MAX_IMAGE_PIXELS", MaximoPixeles_wys);

        // En Windows (equipo de desarrollo) OpenCV se cierra de golpe al repartir el trabajo en
        // varios hilos (comprobado el 22/10/2026). Con uno solo funciona bien y sigue siendo rápido.
        // En el servidor (Linux, Docker) se mantiene el reparto normal.
        if (OperatingSystem.IsWindows())
        {
            Cv2.SetNumThreads(1);
        }
    }
}
