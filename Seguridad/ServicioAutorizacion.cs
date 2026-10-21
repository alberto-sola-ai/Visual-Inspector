using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;

namespace VisualInspector.Seguridad;

/// <summary>
/// Se lanza cuando alguien intenta hacer algo sin permiso. La pantalla muestra un mensaje
/// amable; el detalle queda en el diario de seguridad.
/// </summary>
public sealed class AccesoDenegadoException(string permiso)
    : Exception($"Acceso denegado al permiso {permiso}.")
{
    public string Permiso { get; } = permiso;
}

/// <summary>
/// Comprueba los permisos dentro de los servicios, en el servidor. Ocultar un botón en
/// pantalla es solo comodidad; la protección de verdad está aquí.
/// </summary>
public sealed class ServicioAutorizacion(
    AuthenticationStateProvider estadoSesion,
    IAuthorizationService autorizacion,
    RegistroSeguridad registroSeguridad)
{
    /// <summary>
    /// Mira si la persona conectada tiene el permiso, sin lanzar ningún error.
    /// </summary>
    public async Task<bool> TienePermisoAsync(string permiso)
    {
        var persona = (await estadoSesion.GetAuthenticationStateAsync()).User;
        if (persona.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return (await autorizacion.AuthorizeAsync(persona, null, new RequisitoPermiso(permiso))).Succeeded;
    }

    /// <summary>
    /// Exige el permiso: si la persona no lo tiene, lo apunta en el diario de seguridad
    /// y detiene la operación.
    /// </summary>
    public async Task ExigirPermisoAsync(string permiso)
    {
        if (!await TienePermisoAsync(permiso))
        {
            await registroSeguridad.RegistrarAsync(AccionesSeguridad.AccesoDenegado_wys, $"Denegado ({permiso})");
            throw new AccesoDenegadoException(permiso);
        }
    }
}
