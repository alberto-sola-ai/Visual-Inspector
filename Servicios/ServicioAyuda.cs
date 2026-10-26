using System.Globalization;
using System.Text;
using Markdig;

namespace VisualInspector.Servicios;

/// <summary>
/// Un apartado de la guía de usuario, listo para enseñar en la pantalla Ayuda.
/// </summary>
/// <param name="Id">Nombre corto para el enlace del índice (por ejemplo "que-es-visual-inspector").</param>
/// <param name="Titulo">Título del apartado (el encabezado de segundo nivel de la guía).</param>
/// <param name="Html">El apartado ya convertido a HTML, sin HTML en bruto escrito a mano.</param>
/// <param name="TextoBusqueda">Título y texto en minúsculas y sin acentos, para buscar.</param>
public sealed record ApartadoAyuda(string Id, string Titulo, string Html, string TextoBusqueda);

/// <summary>
/// La guía de usuario entera: su título, su presentación y sus apartados.
/// </summary>
public sealed record GuiaAyuda(string Titulo, string Presentacion, IReadOnlyList<ApartadoAyuda> Apartados);

/// <summary>
/// Lee la guía de usuario (doc/guia-usuario.md) y la prepara para la pantalla Ayuda, como quien
/// corta un manual en capítulos y los pone en un índice. La guía es un archivo del servidor, no
/// la escribe ningún usuario; aun así, no se admite HTML en bruto. Si el archivo cambia, se vuelve
/// a leer sola, así que basta con editarlo.
/// </summary>
public sealed class ServicioAyuda(IWebHostEnvironment entorno, ILogger<ServicioAyuda> registro)
{
    // La guía se edita en doc/ y viaja con la aplicación en esa misma carpeta (ver el .csproj)
    private const string RutaGuia_wys = "doc/guia-usuario.md";

    // Palabras de menos letras que esto no se usan para buscar ("a", "de", "la"…)
    private const int LongitudMinimaPalabra_wys = 2;

    // Markdown con tablas y sin HTML en bruto: lo que haya escrito entre < > se muestra como texto
    private static readonly MarkdownPipeline Conversor_wys = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .DisableHtml()
        .Build();

    private readonly object _candado = new();
    private GuiaAyuda? _guia;
    private DateTime _fechaArchivo;

    /// <summary>
    /// Devuelve la guía, o null si el archivo no está o no se puede leer. Solo la vuelve a leer
    /// si el archivo ha cambiado desde la última vez.
    /// </summary>
    public GuiaAyuda? ObtenerGuia()
    {
        var ruta = Path.Combine(entorno.ContentRootPath, RutaGuia_wys);
        try
        {
            if (!File.Exists(ruta))
                return null;

            var fecha = File.GetLastWriteTimeUtc(ruta);
            lock (_candado)
            {
                if (_guia is null || fecha != _fechaArchivo)
                {
                    _guia = Preparar(File.ReadAllText(ruta, Encoding.UTF8));
                    _fechaArchivo = fecha;
                }
                return _guia;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            registro.LogError(e, "No se ha podido leer la guía de usuario ({Ruta})", RutaGuia_wys);
            return null;
        }
    }

    /// <summary>
    /// Devuelve los apartados que contienen todas las palabras escritas, sin distinguir mayúsculas
    /// ni acentos. Primero van los que las tienen en el título. Sin texto, devuelve todos.
    /// </summary>
    public static IReadOnlyList<ApartadoAyuda> Buscar(GuiaAyuda guia, string? texto)
    {
        var palabras = Normalizar(texto ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length >= LongitudMinimaPalabra_wys)
            .ToList();
        if (palabras.Count == 0)
            return guia.Apartados;

        return guia.Apartados
            .Where(a => palabras.All(a.TextoBusqueda.Contains))
            .OrderByDescending(a => palabras.Count(p => Normalizar(a.Titulo).Contains(p)))
            .ToList();
    }

    /// <summary>
    /// Parte la guía en un título, una presentación y un apartado por cada encabezado de segundo
    /// nivel (##). Lo que va dentro de un apartado (### y demás) se queda con él.
    /// </summary>
    private static GuiaAyuda Preparar(string markdown)
    {
        var titulo = "Ayuda";
        var presentacion = new StringBuilder();
        var apartados = new List<ApartadoAyuda>();
        string? tituloApartado = null;
        var cuerpo = new StringBuilder();

        void CerrarApartado()
        {
            if (tituloApartado is null)
                return;
            var html = Markdown.ToHtml(cuerpo.ToString(), Conversor_wys);
            var texto = Markdown.ToPlainText(cuerpo.ToString(), Conversor_wys);
            apartados.Add(new ApartadoAyuda(CrearId(tituloApartado), tituloApartado, html,
                Normalizar(tituloApartado + " " + texto)));
        }

        foreach (var linea in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            if (linea.StartsWith("## "))
            {
                CerrarApartado();
                tituloApartado = linea[3..].Trim();
                cuerpo.Clear();
            }
            else if (tituloApartado is not null)
            {
                cuerpo.AppendLine(linea);
            }
            else if (linea.StartsWith("# "))
            {
                titulo = linea[2..].Trim();
            }
            else
            {
                presentacion.AppendLine(linea);
            }
        }
        CerrarApartado();

        return new GuiaAyuda(titulo, Markdown.ToHtml(presentacion.ToString(), Conversor_wys), apartados);
    }

    /// <summary>
    /// Pasa a minúsculas y quita los acentos: "Contraseña" y "contrasena" se buscan igual.
    /// </summary>
    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(descompuesto.Length);
        foreach (var letra in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark)
                resultado.Append(char.ToLowerInvariant(letra));
        }
        return resultado.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Convierte un título en un nombre para el enlace: solo letras y cifras, unidas por guiones.
    /// </summary>
    private static string CrearId(string titulo)
    {
        var limpio = new StringBuilder();
        foreach (var letra in Normalizar(titulo))
            limpio.Append(char.IsAsciiLetterOrDigit(letra) ? letra : ' ');
        return string.Join('-', limpio.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
