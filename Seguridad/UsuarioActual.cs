using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace VisualInspector.Seguridad;

/// <summary>
/// Dice quién está usando la aplicación en este momento.
/// </summary>
public interface IUsuarioActual
{
    Task<string> ObtenerNombreAsync();
    Task<string?> ObtenerIdAsync();
}

/// <summary>
/// Averigua quién tiene la sesión abierta, como la recepcionista que mira la tarjeta
/// de quien entra. Si no hay nadie (arranque o procesos automáticos), responde "sistema".
/// </summary>
public sealed class UsuarioActual(
    IServiceProvider servicios,
    IHttpContextAccessor accesoPeticion) : IUsuarioActual
{
    public const string NombreSistema_wys = "sistema";

    /// <summary>
    /// Devuelve el usuario con el que la persona entró (por ejemplo "admin") o "sistema".
    /// </summary>
    public async Task<string> ObtenerNombreAsync()
    {
        var persona = await ObtenerPersonaAsync();
        return persona?.Identity?.IsAuthenticated == true
            ? persona.Identity.Name ?? NombreSistema_wys
            : NombreSistema_wys;
    }

    /// <summary>
    /// Devuelve el identificador interno de la persona, o nada si no hay nadie conectado.
    /// </summary>
    public async Task<string?> ObtenerIdAsync()
    {
        var persona = await ObtenerPersonaAsync();
        return persona?.Identity?.IsAuthenticated == true
            ? persona.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
    }

    /// <summary>
    /// Busca la "tarjeta" de la persona: primero en Blazor y, si no está disponible
    /// (por ejemplo, en el arranque), en la petición web.
    /// </summary>
    private async Task<ClaimsPrincipal?> ObtenerPersonaAsync()
    {
        // Se pide al momento (y no en el constructor) porque fuera de una pantalla Blazor no existe
        var proveedor = servicios.GetService<AuthenticationStateProvider>();
        if (proveedor is not null)
        {
            try
            {
                return (await proveedor.GetAuthenticationStateAsync()).User;
            }
            catch (InvalidOperationException)
            {
                // Fuera de una pantalla Blazor el proveedor aún no tiene datos: se usa la petición web
            }
        }

        return accesoPeticion.HttpContext?.User;
    }
}
