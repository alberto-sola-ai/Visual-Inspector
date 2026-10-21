using Microsoft.AspNetCore.Authorization;

namespace VisualInspector.Seguridad;

/// <summary>
/// La regla "para pasar por aquí necesitas esta llave".
/// </summary>
public sealed class RequisitoPermiso(string permiso) : IAuthorizationRequirement
{
    public string Permiso { get; } = permiso;
}

/// <summary>
/// El portero que mira si la persona tiene la llave pedida. Por defecto no deja pasar a nadie
/// (denegación por defecto); el rol Administrador tiene todas las llaves.
/// </summary>
public sealed class ManejadorPermiso : AuthorizationHandler<RequisitoPermiso>
{
    /// <summary>
    /// Deja pasar solo si la persona es administradora o tiene el permiso concedido,
    /// y nunca mientras tenga pendiente cambiar su contraseña.
    /// </summary>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequisitoPermiso requisito)
    {
        var persona = context.User;

        // Hasta que no cambie la contraseña inicial o restablecida, no puede usar nada
        if (persona.HasClaim(c => c.Type == Permisos.TipoClaimDebeCambiarPassword_wys))
        {
            return Task.CompletedTask;
        }

        if (persona.IsInRole(Permisos.RolAdministrador_wys))
        {
            context.Succeed(requisito);
            return Task.CompletedTask;
        }

        // La sección de Administración es solo del rol Administrador: no se concede como permiso suelto
        if (requisito.Permiso != Permisos.Administracion_wys
            && persona.HasClaim(Permisos.TipoClaimPermiso_wys, requisito.Permiso))
        {
            context.Succeed(requisito);
        }

        return Task.CompletedTask;
    }
}
