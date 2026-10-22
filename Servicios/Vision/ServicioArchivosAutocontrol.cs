using System.Security.Cryptography;
using System.Text.RegularExpressions;
using OpenCvSharp;
using PDFtoImage;
using SkiaSharp;
using VisualInspector.Seguridad;

namespace VisualInspector.Servicios.Vision;

/// <summary>
/// Un archivo ya revisado y listo para usar. Solo vive en memoria mientras la pantalla está
/// abierta; nunca se guarda en disco ni en la base de datos.
/// </summary>
/// <param name="NombreArchivo">Nombre original del archivo en el PC (sin la ruta).</param>
/// <param name="Bytes">Tamaño del archivo.</param>
/// <param name="Huella">Huella SHA-256 (64 caracteres): demuestra después qué archivo exacto se usó.</param>
/// <param name="AnchoPixeles">Ancho de la imagen a 300 ppp.</param>
/// <param name="AltoPixeles">Alto de la imagen a 300 ppp.</param>
/// <param name="VistaJpeg">Imagen reducida para mostrar en pantalla.</param>
/// <param name="Contenido">El archivo tal cual, para los pasos siguientes (localizar y comparar).</param>
public sealed record ArchivoPreparado(
    string NombreArchivo, long Bytes, string Huella, int AnchoPixeles, int AltoPixeles,
    byte[] VistaJpeg, byte[] Contenido)
{
    // A 300 ppp, 1 mm son 300 / 25,4 píxeles
    public double AnchoMilimetros => AnchoPixeles * 25.4 / ServicioArchivosAutocontrol.Resolucion_wys;
    public double AltoMilimetros => AltoPixeles * 25.4 / ServicioArchivosAutocontrol.Resolucion_wys;
}

/// <summary>
/// Litografía y versión leídas del nombre del patrón; cualquiera de las dos puede faltar.
/// </summary>
public sealed record DatosNombrePatron(string? Litografia, string? Version);

/// <summary>
/// El archivo no se puede usar (tipo, tamaño o contenido no válidos). El mensaje es para el usuario.
/// </summary>
public sealed class ArchivoNoValidoException(string mensaje) : Exception(mensaje);

/// <summary>
/// Revisa y prepara los archivos de un autocontrol: el patrón PDF y las muestras escaneadas.
/// Comprueba que cada archivo es lo que dice ser, calcula su huella y lo convierte en imagen a
/// 300 ppp, todo en memoria. Es como la recepción de un laboratorio: mira la etiqueta de cada
/// muestra antes de dejarla pasar.
/// </summary>
public sealed partial class ServicioArchivosAutocontrol(ServicioAutorizacion autorizacion)
{
    // Patrón y muestras se trabajan siempre a 300 ppp (puntos por pulgada)
    public const int Resolucion_wys = 300;
    public const long TamanoMaximoBytes_wys = 50L * 1024 * 1024;   // 50 MB por archivo

    // Límites para que un archivo enorme o mal hecho no agote la memoria del servidor
    public const int LadoMaximoPixeles_wys = 12000;                // ≈ 1 metro a 300 ppp
    private const double LadoMaximoPaginaMm_wys = 600;
    private const int LadoVistaPixeles_wys = 900;                  // tamaño de la vista en pantalla
    private const int CalidadVistaJpeg_wys = 85;

    // El nombre del patrón empieza siempre por la litografía (L + 7 cifras) seguida de la
    // versión (R + cifras). Ejemplo: "L2201989R002 S_CONT_NEO SS SOLVANT 30 ML.pdf"
    [GeneratedRegex(@"^(?<litografia>L\d{7})(?<version>R\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex FormatoNombrePatron();

    [GeneratedRegex(@"^L\d{7}$", RegexOptions.IgnoreCase)]
    private static partial Regex FormatoLitografia();

    /// <summary>
    /// Lee la litografía y la versión del principio del nombre del patrón. Si el nombre no sigue
    /// el formato, devuelve las dos vacías y la pantalla deja escribirlas a mano.
    /// </summary>
    public static DatosNombrePatron LeerNombrePatron(string nombreArchivo)
    {
        var coincidencia = FormatoNombrePatron().Match(Path.GetFileName(nombreArchivo).Trim());
        return coincidencia.Success
            ? new(coincidencia.Groups["litografia"].Value.ToUpperInvariant(), coincidencia.Groups["version"].Value.ToUpperInvariant())
            : new(null, null);
    }

    /// <summary>
    /// Comprueba que una litografía escrita a mano tiene el formato correcto (L + 7 cifras).
    /// </summary>
    public static bool EsLitografiaValida(string? litografia) =>
        litografia is not null && FormatoLitografia().IsMatch(litografia.Trim());

    /// <summary>
    /// Revisa el patrón PDF y dibuja su primera página a 300 ppp.
    /// </summary>
    public async Task<ArchivoPreparado> PrepararPatronAsync(Stream contenido, string nombreArchivo)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);
        var datos = await LeerConLimiteAsync(contenido);

        // Se mira el contenido real, no la extensión: un PDF siempre empieza por "%PDF-"
        if (datos.Length < 5 || datos[0] != '%' || datos[1] != 'P' || datos[2] != 'D' || datos[3] != 'F' || datos[4] != '-')
        {
            throw new ArchivoNoValidoException("El patrón debe ser un archivo PDF.");
        }

        // El trabajo pesado se hace fuera del hilo de la pantalla para que no se quede congelada
        return await Task.Run(() =>
        {
            using var imagen = DibujarPatron(datos);
            return CrearResultado(nombreArchivo, datos, imagen);
        });
    }

    /// <summary>
    /// Revisa una muestra escaneada (JPG, PNG o TIFF a 300 ppp).
    /// </summary>
    public async Task<ArchivoPreparado> PrepararMuestraAsync(Stream contenido, string nombreArchivo)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);
        var datos = await LeerConLimiteAsync(contenido);

        if (!EsJpeg(datos) && !EsPng(datos) && !EsTiff(datos))
        {
            throw new ArchivoNoValidoException("La muestra debe ser una imagen JPG, PNG o TIFF.");
        }

        return await Task.Run(() =>
        {
            using var imagen = LeerImagen(datos);
            return CrearResultado(nombreArchivo, datos, imagen);
        });
    }

    /// <summary>
    /// Convierte la primera página del PDF en imagen en color a 300 ppp, como quien fotocopia el
    /// patrón a tamaño real. La usan también los pasos de localizar y comparar.
    /// </summary>
    public static Mat DibujarPatron(byte[] datos)
    {
        // La biblioteca de PDF solo funciona en ciertos sistemas; el servidor es Linux (Docker)
        // o Windows (desarrollo)
        if (OperatingSystem.IsLinux() || OperatingSystem.IsWindows())
        {
            return DibujarPatronEnSistemaAdmitido(datos);
        }
        throw new PlatformNotSupportedException("La conversión de PDF solo funciona en Linux o Windows.");
    }

    /// <summary>
    /// Lee una imagen escaneada en color. La usan también los pasos de localizar y comparar.
    /// </summary>
    public static Mat LeerImagen(byte[] datos)
    {
        var imagen = Cv2.ImDecode(datos, ImreadModes.Color);
        if (imagen.Empty())
        {
            imagen.Dispose();
            throw new ArchivoNoValidoException("No se ha podido leer la imagen de la muestra.");
        }
        if (imagen.Width > LadoMaximoPixeles_wys || imagen.Height > LadoMaximoPixeles_wys)
        {
            imagen.Dispose();
            throw new ArchivoNoValidoException("La imagen de la muestra es demasiado grande.");
        }
        return imagen;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static Mat DibujarPatronEnSistemaAdmitido(byte[] datos)
    {
        try
        {
            var tamano = Conversion.GetPageSize(datos, 0);
            // El tamaño de página viene en puntos (1/72 de pulgada)
            var anchoMm = tamano.Width * 25.4 / 72;
            var altoMm = tamano.Height * 25.4 / 72;
            if (anchoMm > LadoMaximoPaginaMm_wys || altoMm > LadoMaximoPaginaMm_wys)
            {
                throw new ArchivoNoValidoException("La página del patrón es demasiado grande (máximo 600 mm por lado).");
            }

            using var pagina = Conversion.ToImage(datos, 0, options: new RenderOptions
            {
                Dpi = Resolucion_wys,
                WithAnnotations = true,
                BackgroundColor = SKColors.White
            });
            // De la imagen de la biblioteca de PDF a la de OpenCV (se mantiene el color)
            using var png = pagina.Encode(SKEncodedImageFormat.Png, 100);
            return Cv2.ImDecode(png.ToArray(), ImreadModes.Color);
        }
        catch (ArchivoNoValidoException)
        {
            throw;
        }
        catch (Exception)
        {
            // PDF dañado, protegido con contraseña o con un formato que no se puede leer
            throw new ArchivoNoValidoException("No se ha podido leer el PDF del patrón. Comprueba que no está dañado ni protegido con contraseña.");
        }
    }

    /// <summary>
    /// Copia el archivo a memoria, cortando si pasa de 50 MB.
    /// </summary>
    private static async Task<byte[]> LeerConLimiteAsync(Stream contenido)
    {
        using var memoria = new MemoryStream();
        var bufer = new byte[81920];
        int leidos;
        while ((leidos = await contenido.ReadAsync(bufer)) > 0)
        {
            if (memoria.Length + leidos > TamanoMaximoBytes_wys)
            {
                throw new ArchivoNoValidoException("El archivo pasa de 50 MB.");
            }
            await memoria.WriteAsync(bufer.AsMemory(0, leidos));
        }

        if (memoria.Length == 0)
        {
            throw new ArchivoNoValidoException("El archivo está vacío.");
        }
        return memoria.ToArray();
    }

    /// <summary>
    /// Calcula la huella del archivo y prepara la vista reducida (en color) para la pantalla.
    /// </summary>
    private static ArchivoPreparado CrearResultado(string nombreArchivo, byte[] datos, Mat imagen)
    {
        var huella = Convert.ToHexString(SHA256.HashData(datos)).ToLowerInvariant();
        var vista = VistaReducida(imagen, LadoVistaPixeles_wys);

        // Solo se conserva el nombre, sin la ruta del PC
        var nombre = Path.GetFileName(nombreArchivo);
        return new ArchivoPreparado(nombre, datos.LongLength, huella, imagen.Width, imagen.Height, vista, datos);
    }

    /// <summary>
    /// Reduce una imagen para la pantalla (manteniendo la proporción y el color) y la pasa a JPEG,
    /// para no mandar 50 MB al navegador.
    /// </summary>
    public static byte[] VistaReducida(Mat imagen, int ladoMaximo)
    {
        var escala = Math.Min(1.0, (double)ladoMaximo / Math.Max(imagen.Width, imagen.Height));
        using var reducida = new Mat();
        Cv2.Resize(imagen, reducida, new Size(Math.Max(1, (int)(imagen.Width * escala)), Math.Max(1, (int)(imagen.Height * escala))),
            0, 0, InterpolationFlags.Area);
        Cv2.ImEncode(".jpg", reducida, out var jpeg, new ImageEncodingParam(ImwriteFlags.JpegQuality, CalidadVistaJpeg_wys));
        return jpeg;
    }

    private static bool EsJpeg(byte[] d) => d.Length > 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF;

    private static bool EsPng(byte[] d) =>
        d.Length > 8 && d[0] == 0x89 && d[1] == 0x50 && d[2] == 0x4E && d[3] == 0x47;

    // TIFF empieza por "II*" + 0 (orden Intel) o "MM" + 0 + "*" (orden Motorola)
    private static bool EsTiff(byte[] d) =>
        d.Length > 4 && ((d[0] == 0x49 && d[1] == 0x49 && d[2] == 0x2A && d[3] == 0x00)
                      || (d[0] == 0x4D && d[1] == 0x4D && d[2] == 0x00 && d[3] == 0x2A));
}
