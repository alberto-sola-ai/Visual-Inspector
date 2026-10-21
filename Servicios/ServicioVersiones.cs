using System.Text.Json;
using VisualInspector.Seguridad;

namespace VisualInspector.Servicios;

/// <summary>
/// Una pieza de la aplicación y si conviene actualizarla.
/// </summary>
public sealed record ComponenteVersion(
    string Nombre, string Descripcion, string Instalada, string Ultima, string Recomendacion);

/// <summary>
/// El contenido de recursos/versiones.json: la lista de piezas y la fecha en que se revisó.
/// </summary>
public sealed record InformeVersiones(DateTime Generado, List<ComponenteVersion> Componentes);

/// <summary>
/// Lee el informe de versiones que se genera al publicar cada versión (script
/// generar-versiones.ps1), sin que la aplicación tenga que salir a internet.
/// Es como la hoja de revisión del coche: dice qué piezas lleva y cuáles conviene cambiar.
/// </summary>
public sealed class ServicioVersiones(IWebHostEnvironment entorno, ServicioAutorizacion autorizacion)
{
    public const string RutaInforme_wys = "recursos/versiones.json";

    private static readonly JsonSerializerOptions OpcionesJson_wys = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Devuelve el informe (solo administradores), o null si todavía no se ha generado.
    /// </summary>
    public async Task<InformeVersiones?> ObtenerAsync()
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        var ruta = Path.Combine(entorno.ContentRootPath, RutaInforme_wys);
        if (!File.Exists(ruta))
        {
            return null;
        }

        await using var archivo = File.OpenRead(ruta);
        return await JsonSerializer.DeserializeAsync<InformeVersiones>(archivo, OpcionesJson_wys);
    }
}
