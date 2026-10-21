using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VisualInspector.Datos;
using VisualInspector.Seguridad;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Rutas que necesitan las páginas de cuenta y que no son pantallas.
/// Se han quitado las de la plantilla que la norma WYS no usa (inicio de sesión con cuentas
/// externas, passkeys y descarga de datos personales): solo queda cerrar sesión.
/// </summary>
internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Añade la ruta /Account/Logout, que cierra la sesión y lo apunta en el diario de seguridad.
    /// </summary>
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/Logout", async (
            HttpContext contexto,
            [FromServices] IAntiforgery antifalsificacion,
            [FromServices] SignInManager<UsuarioAplicacion> signInManager,
            [FromServices] RegistroSeguridad registroSeguridad,
            [FromServices] IUsuarioActual usuarioActual) =>
        {
            // Comprueba que la petición viene de nuestro propio formulario (protección CSRF):
            // así otra web no puede cerrar la sesión de alguien sin que lo sepa
            await antifalsificacion.ValidateRequestAsync(contexto);

            // Se toma el ID antes de cerrar, porque después ya no hay nadie conectado
            var idUsuario = await usuarioActual.ObtenerIdAsync();
            await signInManager.SignOutAsync();
            await registroSeguridad.RegistrarAsync(AccionesSeguridad.CierreSesion_wys, "Correcto", idUsuario);
            // Siempre vuelve a la pantalla de entrada (no se acepta una dirección de vuelta externa)
            return TypedResults.LocalRedirect("~/Account/Login");
        });

        return accountGroup;
    }
}
