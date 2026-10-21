using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VisualInspector.Datos;

namespace VisualInspector.Seguridad;

/// <summary>
/// Prepara la "tarjeta de sesión" de cada persona al entrar: además del usuario, su rol y sus
/// permisos, apunta su nombre completo y si tiene pendiente cambiar la contraseña.
/// </summary>
public sealed class FabricaSesionUsuario(
    UserManager<UsuarioAplicacion> gestorUsuarios,
    RoleManager<IdentityRole> gestorRoles,
    IOptions<IdentityOptions> opciones)
    : UserClaimsPrincipalFactory<UsuarioAplicacion, IdentityRole>(gestorUsuarios, gestorRoles, opciones)
{
    /// <summary>
    /// Añade a la tarjeta estándar los datos propios de WYS.
    /// </summary>
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(UsuarioAplicacion user)
    {
        var identidad = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.NombreCompleto))
        {
            identidad.AddClaim(new Claim(Permisos.TipoClaimNombreCompleto_wys, user.NombreCompleto));
        }

        if (user.DebeCambiarPassword)
        {
            identidad.AddClaim(new Claim(Permisos.TipoClaimDebeCambiarPassword_wys, "si"));
        }

        return identidad;
    }
}

/// <summary>
/// El encargado de dejar entrar: igual que el de serie, pero además no deja pasar
/// a los usuarios desactivados.
/// </summary>
public sealed class GestorEntrada(
    UserManager<UsuarioAplicacion> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<UsuarioAplicacion> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<UsuarioAplicacion>> logger,
    Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider schemes,
    IUserConfirmation<UsuarioAplicacion> confirmation)
    : SignInManager<UsuarioAplicacion>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    /// <summary>
    /// Un usuario desactivado no puede entrar, aunque su contraseña sea correcta.
    /// </summary>
    public override async Task<bool> CanSignInAsync(UsuarioAplicacion user)
    {
        return user.Activo && await base.CanSignInAsync(user);
    }
}
