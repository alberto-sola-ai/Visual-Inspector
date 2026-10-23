using System.Globalization;
using System.Text.RegularExpressions;
using VisualInspector.Seguridad;

namespace VisualInspector.Servicios;

/// <summary>
/// Los dos diarios de la carpeta log/: el de errores (err-log) y el de seguridad (Log-Wys-Seg).
/// </summary>
public enum TipoRegistro
{
    Errores,
    Seguridad
}

/// <summary>
/// Lo leído de un diario: sus apuntes, del más reciente al más antiguo.
/// </summary>
/// <param name="Total">Apuntes que cumplen la búsqueda (puede haber más de los que se enseñan).</param>
/// <param name="EstaRecortado">Hay más apuntes de los que se enseñan.</param>
public sealed record LecturaRegistro(IReadOnlyList<string> Apuntes, int Total, bool EstaRecortado);

/// <summary>
/// Deja al administrador leer los diarios de la aplicación para investigar errores, como quien
/// consulta el libro de incidencias. Solo lectura y solo el administrador; cada consulta queda
/// apuntada en el diario de seguridad, porque esos diarios guardan usuarios e IP.
/// </summary>
public sealed partial class ServicioRegistros(
    IWebHostEnvironment entorno,
    ServicioAutorizacion autorizacion,
    RegistroSeguridad registroSeguridad)
{
    // Como mucho se enseñan los 1.000 apuntes más recientes para que la pantalla no se cuelgue
    public const int MaximoApuntes_wys = 1000;

    // Cada apunte empieza por la fecha y la hora (dd/MM/yyyy HH:mm:ss); las líneas que no, son
    // la continuación del apunte anterior (por ejemplo el detalle técnico de un error)
    [GeneratedRegex(@"^\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2}")]
    private static partial Regex InicioApunte();

    /// <summary>
    /// Nombre con el que empieza cada archivo del diario (luego va la fecha AAAAMMDD y ".log").
    /// </summary>
    public static string Prefijo(TipoRegistro tipo) => tipo == TipoRegistro.Seguridad ? "Log-Wys-Seg-" : "err-log-";

    /// <summary>
    /// Dice qué días hay guardados de un diario, del más reciente al más antiguo.
    /// </summary>
    public async Task<IReadOnlyList<DateOnly>> ListarDiasAsync(TipoRegistro tipo)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        var carpeta = CarpetaLog();
        if (!Directory.Exists(carpeta))
        {
            return [];
        }

        var prefijo = Prefijo(tipo);
        return Directory.EnumerateFiles(carpeta, prefijo + "*.log")
            .Select(r => Path.GetFileNameWithoutExtension(r)[prefijo.Length..])
            .Select(f => DateOnly.TryParseExact(f, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia) ? dia : (DateOnly?)null)
            .Where(d => d is not null)
            .Select(d => d!.Value)
            .OrderDescending()
            .ToList();
    }

    /// <summary>
    /// Lee los apuntes de un día, del más reciente al más antiguo. Si se indica un texto, solo
    /// los que lo contienen. Apunta la consulta en el diario de seguridad.
    /// </summary>
    public async Task<LecturaRegistro> LeerAsync(TipoRegistro tipo, DateOnly dia, string? texto)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        // El nombre del archivo lo monta el servidor con el tipo y la fecha: nunca llega de fuera
        var nombre = $"{Prefijo(tipo)}{dia:yyyyMMdd}.log";
        await registroSeguridad.RegistrarAsync(AccionesSeguridad.ConsultaRegistro_wys, $"Correcto ({nombre})");

        var ruta = Path.Combine(CarpetaLog(), nombre);
        if (!File.Exists(ruta))
        {
            return new([], 0, false);
        }

        // El diario puede estar escribiéndose a la vez: se abre sin bloquearlo
        string[] lineas;
        await using (var flujo = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        using (var lector = new StreamReader(flujo))
        {
            lineas = (await lector.ReadToEndAsync()).Split('\n');
        }

        // Junta cada apunte con sus líneas de continuación
        var apuntes = new List<string>();
        foreach (var linea in lineas.Select(l => l.TrimEnd('\r')))
        {
            if (InicioApunte().IsMatch(linea) || apuntes.Count == 0)
                apuntes.Add(linea);
            else if (linea.Length > 0)
                apuntes[^1] += "\n" + linea;
        }

        var filtro = (texto ?? "").Trim();
        var encontrados = apuntes
            .Where(a => a.Length > 0 && (filtro.Length == 0 || a.Contains(filtro, StringComparison.OrdinalIgnoreCase)))
            .Reverse()
            .ToList();

        return new(encontrados.Take(MaximoApuntes_wys).ToList(), encontrados.Count, encontrados.Count > MaximoApuntes_wys);
    }

    // La carpeta log/ está junto a la aplicación (en Docker, /app/log)
    private string CarpetaLog() => Path.Combine(entorno.ContentRootPath, "log");
}
