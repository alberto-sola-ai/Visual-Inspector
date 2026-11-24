using Microsoft.EntityFrameworkCore;
using VisualInspector.Datos;
using VisualInspector.Seguridad;

namespace VisualInspector.Servicios.Vision;

/// <summary>
/// Lo encontrado en la carpeta de patrones para un autocontrol.
/// </summary>
/// <param name="Especialidad">Especialidad de la orden (E2201829), o null si no tiene (orden fuera de planificación).</param>
/// <param name="Codigo">Lo que se busca en el nombre del PDF: la especialidad sin la E (2201829), o null.</param>
/// <param name="EsCarpetaDisponible">Se ha podido leer la carpeta de patrones.</param>
/// <param name="Archivos">Nombres de los PDF que contienen el código, la versión más alta primero.</param>
public sealed record PatronesEncontrados(string? Especialidad, string? Codigo, bool EsCarpetaDisponible, IReadOnlyList<string> Archivos)
{
    public static readonly PatronesEncontrados Ninguno_wys = new(null, null, false, []);
}

/// <summary>
/// Busca el patrón de un autocontrol en la carpeta de patrones (una carpeta de red): enseña solo los
/// PDF cuyo nombre lleva la especialidad de la orden sin la E (decisión de Alberto, 24/11/2026). Si la
/// orden no tiene especialidad o la carpeta no se puede leer, no da error: el operario elige el archivo
/// desde su PC como siempre. Solo lee: nunca escribe ni borra nada en la carpeta.
/// </summary>
public sealed class BuscadorPatrones(
    IConfiguration configuracion,
    IWebHostEnvironment entorno,
    IDbContextFactory<AppDbContext> fabricaDatos,
    ServicioAutorizacion autorizacion,
    ServicioArchivosAutocontrol archivos,
    ServicioParametros parametros,
    ILogger<BuscadorPatrones> registro)
{
    // Carpeta de patrones de la instalación (appsettings o, en Docker, variable Patrones__Carpeta). El
    // parámetro CarpetaPatrones, si tiene valor, manda sobre ella. Las dos vacías = sin carpeta
    public const string SeccionCarpeta_wys = "Patrones:Carpeta";

    // Como mucho se enseñan estos archivos; con la especialidad suele haber uno o dos (versiones R001, R002…)
    public const int MaximoArchivos_wys = 20;

    // Si la carpeta de red no contesta en este tiempo, se sigue sin ella en lugar de dejar la pantalla esperando
    private static readonly TimeSpan TiempoMaximoCarpeta_wys = TimeSpan.FromSeconds(10);

    // Menos cifras que esto no basta para saber qué patrón es (encontraría demasiados)
    private const int CifrasMinimasCodigo_wys = 4;

    /// <summary>
    /// Saca de la especialidad lo que se busca en el nombre del PDF: sus cifras, sin la E
    /// (E2201829 → 2201829). Devuelve null si no hay especialidad o tiene muy pocas cifras.
    /// </summary>
    public static string? CodigoBusqueda(string? especialidad)
    {
        var cifras = new string((especialidad ?? "").Where(char.IsAsciiDigit).ToArray());
        return cifras.Length >= CifrasMinimasCodigo_wys ? cifras : null;
    }

    /// <summary>
    /// De todos los nombres de la carpeta, se queda con los PDF que llevan el código, como quien pasa el
    /// dedo por las etiquetas de un archivador. La versión más alta (R002 antes que R001) va primero.
    /// </summary>
    public static IReadOnlyList<string> Filtrar(IEnumerable<string> nombres, string codigo) =>
        nombres
            .Where(n => n.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && n.Contains(codigo, StringComparison.Ordinal))
            .OrderByDescending(n => n, StringComparer.OrdinalIgnoreCase)
            .Take(MaximoArchivos_wys)
            .ToList();

    /// <summary>
    /// Busca en la carpeta los patrones de la orden del autocontrol. Nunca falla: si no hay especialidad
    /// o la carpeta no se puede leer, lo dice en el resultado para que la pantalla ofrezca elegir a mano.
    /// </summary>
    public async Task<PatronesEncontrados> BuscarAsync(int autocontrolId)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var especialidad = await datos.Autocontroles.AsNoTracking()
            .Where(a => a.Id == autocontrolId)
            .Select(a => a.OrdenFabricacion.Especialidad)
            .FirstOrDefaultAsync();

        var codigo = CodigoBusqueda(especialidad);
        if (codigo is null)
        {
            return new(especialidad, null, false, []);
        }

        var nombres = await ListarCarpetaAsync();
        return nombres is null
            ? new(especialidad, codigo, false, [])
            : new(especialidad, codigo, true, Filtrar(nombres, codigo));
    }

    /// <summary>
    /// Abre uno de los patrones encontrados y lo prepara igual que si se hubiera elegido desde el PC.
    /// Solo se admite un nombre de la lista encontrada para esta orden (Zero Trust: así nadie puede pedir
    /// otro archivo ni salirse de la carpeta).
    /// </summary>
    public async Task<ArchivoPreparado> AbrirAsync(int autocontrolId, string nombreArchivo)
    {
        var encontrados = await BuscarAsync(autocontrolId);
        var carpeta = await CarpetaAsync();
        if (carpeta is null || !encontrados.Archivos.Contains(nombreArchivo, StringComparer.Ordinal))
        {
            throw new ArchivoNoValidoException("Ese patrón ya no está en la carpeta. Vuelve a buscarlo o elígelo desde tu PC.");
        }

        try
        {
            await using var contenido = new FileStream(Path.Combine(carpeta, nombreArchivo), FileMode.Open, FileAccess.Read,
                FileShare.Read, bufferSize: 81920, useAsync: true);
            if (contenido.Length > ServicioArchivosAutocontrol.TamanoMaximoBytes_wys)
            {
                throw new ArchivoNoValidoException("El archivo pasa de 50 MB.");
            }
            return await archivos.PrepararPatronAsync(contenido, nombreArchivo);
        }
        catch (IOException error)
        {
            registro.LogWarning(error, "Carpeta de patrones: no se ha podido abrir un patrón.");
            throw new ArchivoNoValidoException("No se ha podido abrir el patrón de la carpeta. Inténtalo de nuevo o elígelo desde tu PC.");
        }
        catch (UnauthorizedAccessException error)
        {
            registro.LogWarning(error, "Carpeta de patrones: sin permiso para abrir un patrón.");
            throw new ArchivoNoValidoException("No se ha podido abrir el patrón de la carpeta. Elígelo desde tu PC.");
        }
    }

    /// <summary>
    /// La carpeta de patrones: la del parámetro CarpetaPatrones (Administración) si tiene valor y es
    /// válida; si no, la de la instalación (si es relativa, dentro de la aplicación). Null si no hay ninguna.
    /// </summary>
    private async Task<string?> CarpetaAsync()
    {
        var delParametro = (await parametros.ObtenerValorAsync(ServicioParametros.CarpetaPatrones_wys, "")).Trim();
        if (delParametro.Length > 0 && ServicioParametros.ValidarCarpetaPatrones(delParametro) is null)
        {
            return delParametro;
        }
        var deLaInstalacion = (configuracion[SeccionCarpeta_wys] ?? "").Trim();
        return deLaInstalacion.Length == 0 ? null : Path.Combine(entorno.ContentRootPath, deLaInstalacion);
    }

    /// <summary>
    /// Lee los nombres de los PDF de la carpeta. Si no existe, no contesta a tiempo o no deja leer,
    /// devuelve null y lo apunta en log/err-log, sin romper la pantalla.
    /// </summary>
    private async Task<IReadOnlyList<string>?> ListarCarpetaAsync()
    {
        var carpeta = await CarpetaAsync();
        if (carpeta is null)
        {
            return null;
        }

        // La lectura de una carpeta de red puede quedarse colgada: se hace aparte y con tiempo máximo
        var lectura = Task.Run(() => Directory.Exists(carpeta)
            ? Directory.EnumerateFiles(carpeta, "*.pdf", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).OfType<string>().ToList()
            : null);
        try
        {
            var resultado = await lectura.WaitAsync(TiempoMaximoCarpeta_wys);
            // Vacía cuenta como no disponible: suele ser la carpeta de red sin montar en el servidor
            if (resultado is null or { Count: 0 })
            {
                registro.LogWarning("Carpeta de patrones: no existe, está vacía o no se ve desde el servidor.");
                return null;
            }
            return resultado;
        }
        catch (TimeoutException)
        {
            registro.LogWarning("Carpeta de patrones: no ha contestado en {Segundos} segundos.", TiempoMaximoCarpeta_wys.TotalSeconds);
            return null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            registro.LogWarning(error, "Carpeta de patrones: no se ha podido leer.");
            return null;
        }
    }
}
