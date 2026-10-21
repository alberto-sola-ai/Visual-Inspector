using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VisualInspector.Datos;
using VisualInspector.Seguridad;

namespace VisualInspector.Servicios;

/// <summary>
/// Una fila del listado de usuarios.
/// </summary>
public sealed record FichaUsuario(
    string Id, string Usuario, string NombreCompleto, string Rol,
    bool Activo, bool Bloqueado, DateTime? UltimoAcceso);

/// <summary>
/// Todo lo que se ve en la ficha de un usuario, incluidos sus permisos y quién lo creó o cambió.
/// </summary>
public sealed record DatosUsuario(
    FichaUsuario Ficha, IReadOnlySet<string> Permisos,
    string CreadoPor, DateTime FechaCreacion, string? ModificadoPor, DateTime? FechaModificacion);

/// <summary>
/// Gestión de usuarios para la sección de Administración: crear, editar, activar, desbloquear,
/// restablecer contraseñas y repartir permisos. Todo exige ser administrador, comprobado aquí
/// en el servidor, y todo cambio se apunta en el diario de seguridad.
/// Los métodos que cambian algo devuelven un mensaje de error para el usuario, o null si va bien.
/// </summary>
public sealed partial class ServicioUsuarios(
    UserManager<UsuarioAplicacion> gestorUsuarios,
    ServicioAutorizacion autorizacion,
    IUsuarioActual usuarioActual,
    RegistroSeguridad registroSeguridad)
{
    public static readonly IReadOnlyList<string> RolesDisponibles_wys =
        [Permisos.RolUsuario_wys, Permisos.RolAdministrador_wys];

    // Usuario para entrar: minúsculas, números, punto, guion o guion bajo, de 3 a 50 caracteres
    [GeneratedRegex("^[a-z0-9._-]{3,50}$")]
    private static partial Regex FormatoUsuario();

    /// <summary>
    /// Devuelve la lista de todos los usuarios, ordenada por nombre.
    /// </summary>
    public async Task<List<FichaUsuario>> ListarAsync()
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        var usuarios = await gestorUsuarios.Users.AsNoTracking().OrderBy(u => u.NombreCompleto).ToListAsync();
        var administradores = (await gestorUsuarios.GetUsersInRoleAsync(Permisos.RolAdministrador_wys))
            .Select(u => u.Id).ToHashSet();

        return usuarios.Select(u => CrearFicha(u, administradores.Contains(u.Id))).ToList();
    }

    /// <summary>
    /// Devuelve la ficha completa de un usuario, o null si no existe.
    /// </summary>
    public async Task<DatosUsuario?> ObtenerAsync(string id)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        var usuario = await gestorUsuarios.FindByIdAsync(id);
        if (usuario is null)
        {
            return null;
        }

        var esAdministrador = await gestorUsuarios.IsInRoleAsync(usuario, Permisos.RolAdministrador_wys);
        var permisos = (await gestorUsuarios.GetClaimsAsync(usuario))
            .Where(c => c.Type == Permisos.TipoClaimPermiso_wys)
            .Select(c => c.Value)
            .ToHashSet();

        return new DatosUsuario(CrearFicha(usuario, esAdministrador), permisos,
            usuario.CreadoPor, usuario.FechaCreacion, usuario.ModificadoPor, usuario.FechaModificacion);
    }

    /// <summary>
    /// Da de alta un usuario nuevo con una contraseña temporal que tendrá que cambiar al entrar.
    /// Nace sin permisos (denegación por defecto): el administrador se los concede después.
    /// </summary>
    public async Task<string?> CrearAsync(string usuario, string nombreCompleto, string rol, string passwordTemporal)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        usuario = (usuario ?? "").Trim().ToLowerInvariant();
        nombreCompleto = (nombreCompleto ?? "").Trim();
        var error = ValidarDatos(usuario, nombreCompleto, rol);
        if (error is not null)
        {
            return error;
        }

        if (await gestorUsuarios.FindByNameAsync(usuario) is not null)
        {
            return "Ya existe un usuario con ese nombre.";
        }

        var nuevo = new UsuarioAplicacion
        {
            UserName = usuario,
            NombreCompleto = nombreCompleto,
            Activo = true,
            DebeCambiarPassword = true
        };

        var resultado = await gestorUsuarios.CreateAsync(nuevo, passwordTemporal ?? "");
        if (!resultado.Succeeded)
        {
            return string.Join(" ", resultado.Errors.Select(e => e.Description));
        }

        await gestorUsuarios.AddToRoleAsync(nuevo, rol);
        await registroSeguridad.RegistrarAsync(AccionesSeguridad.AltaUsuario_wys, $"Correcto (rol {rol}, afectado {nuevo.Id})");
        return null;
    }

    /// <summary>
    /// Cambia el nombre completo y el rol de un usuario.
    /// </summary>
    public async Task<string?> GuardarDatosAsync(string id, string nombreCompleto, string rol)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        var usuario = await gestorUsuarios.FindByIdAsync(id);
        if (usuario is null)
        {
            return "El usuario ya no existe.";
        }

        nombreCompleto = (nombreCompleto ?? "").Trim();
        var error = ValidarDatos(usuario.UserName!, nombreCompleto, rol);
        if (error is not null)
        {
            return error;
        }

        var eraAdministrador = await gestorUsuarios.IsInRoleAsync(usuario, Permisos.RolAdministrador_wys);
        var seraAdministrador = rol == Permisos.RolAdministrador_wys;

        // Protección: nadie se quita a sí mismo el rol de administrador, y siempre queda al menos uno
        if (eraAdministrador && !seraAdministrador)
        {
            if (await EsLaPersonaConectadaAsync(usuario))
            {
                return "No puedes quitarte a ti mismo el rol de administrador.";
            }
            if (await ContarAdministradoresActivosAsync() <= 1 && usuario.Activo)
            {
                return "Debe quedar al menos un administrador activo.";
            }
        }

        usuario.NombreCompleto = nombreCompleto;
        var resultado = await gestorUsuarios.UpdateAsync(usuario);
        if (!resultado.Succeeded)
        {
            return string.Join(" ", resultado.Errors.Select(e => e.Description));
        }

        if (eraAdministrador != seraAdministrador)
        {
            var rolAnterior = eraAdministrador ? Permisos.RolAdministrador_wys : Permisos.RolUsuario_wys;
            await gestorUsuarios.RemoveFromRoleAsync(usuario, rolAnterior);
            await gestorUsuarios.AddToRoleAsync(usuario, rol);
            // Renueva el sello: la sesión abierta de esa persona deja de valer y tendrá que volver a entrar
            await gestorUsuarios.UpdateSecurityStampAsync(usuario);
            await registroSeguridad.RegistrarAsync(AccionesSeguridad.CambioRol_wys, $"Correcto ({rolAnterior} -> {rol}, afectado {usuario.Id})");
        }
        else
        {
            await registroSeguridad.RegistrarAsync(AccionesSeguridad.ModificacionUsuario_wys, $"Correcto (afectado {usuario.Id})");
        }

        return null;
    }

    /// <summary>
    /// Activa o desactiva un usuario. Desactivado no puede entrar, pero no se borra
    /// para no perder el rastro de lo que hizo.
    /// </summary>
    public async Task<string?> CambiarActivoAsync(string id, bool activo)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        var usuario = await gestorUsuarios.FindByIdAsync(id);
        if (usuario is null)
        {
            return "El usuario ya no existe.";
        }

        if (!activo)
        {
            if (await EsLaPersonaConectadaAsync(usuario))
            {
                return "No puedes desactivarte a ti mismo.";
            }
            if (await gestorUsuarios.IsInRoleAsync(usuario, Permisos.RolAdministrador_wys)
                && await ContarAdministradoresActivosAsync() <= 1)
            {
                return "Debe quedar al menos un administrador activo.";
            }
        }

        usuario.Activo = activo;
        await gestorUsuarios.UpdateAsync(usuario);
        await gestorUsuarios.UpdateSecurityStampAsync(usuario);

        var accion = activo ? AccionesSeguridad.ActivacionUsuario_wys : AccionesSeguridad.DesactivacionUsuario_wys;
        await registroSeguridad.RegistrarAsync(accion, $"Correcto (afectado {usuario.Id})");
        return null;
    }

    /// <summary>
    /// Quita el bloqueo de 15 minutos antes de tiempo y pone a cero los intentos fallidos.
    /// </summary>
    public async Task<string?> DesbloquearAsync(string id)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        var usuario = await gestorUsuarios.FindByIdAsync(id);
        if (usuario is null)
        {
            return "El usuario ya no existe.";
        }

        await gestorUsuarios.SetLockoutEndDateAsync(usuario, null);
        await gestorUsuarios.ResetAccessFailedCountAsync(usuario);
        await registroSeguridad.RegistrarAsync(AccionesSeguridad.Desbloqueo_wys, $"Correcto (afectado {usuario.Id})");
        return null;
    }

    /// <summary>
    /// Pone una contraseña temporal a un usuario que ha olvidado la suya.
    /// La tendrá que cambiar en cuanto entre.
    /// </summary>
    public async Task<string?> RestablecerPasswordAsync(string id, string passwordTemporal)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        var usuario = await gestorUsuarios.FindByIdAsync(id);
        if (usuario is null)
        {
            return "El usuario ya no existe.";
        }

        var token = await gestorUsuarios.GeneratePasswordResetTokenAsync(usuario);
        var resultado = await gestorUsuarios.ResetPasswordAsync(usuario, token, passwordTemporal ?? "");
        if (!resultado.Succeeded)
        {
            return string.Join(" ", resultado.Errors.Select(e => e.Description));
        }

        usuario.DebeCambiarPassword = true;
        await gestorUsuarios.UpdateAsync(usuario);
        await gestorUsuarios.UpdateSecurityStampAsync(usuario);
        await registroSeguridad.RegistrarAsync(AccionesSeguridad.RestablecerPassword_wys, $"Correcto (afectado {usuario.Id})");
        return null;
    }

    /// <summary>
    /// Deja al usuario exactamente con los permisos marcados: concede los nuevos y retira los
    /// que se hayan desmarcado. Cada concesión y retirada queda apuntada.
    /// </summary>
    public async Task<string?> GuardarPermisosAsync(string id, IEnumerable<string> permisosMarcados)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        var usuario = await gestorUsuarios.FindByIdAsync(id);
        if (usuario is null)
        {
            return "El usuario ya no existe.";
        }

        // Solo se aceptan permisos del catálogo: cualquier otro valor se ignora
        var validos = Permisos.Catalogo_wys.Select(p => p.Codigo).ToHashSet();
        var deseados = permisosMarcados.Where(validos.Contains).ToHashSet();

        var actuales = (await gestorUsuarios.GetClaimsAsync(usuario))
            .Where(c => c.Type == Permisos.TipoClaimPermiso_wys)
            .ToList();
        var codigosActuales = actuales.Select(c => c.Value).ToHashSet();

        var aRetirar = actuales.Where(c => !deseados.Contains(c.Value)).ToList();
        var aConceder = deseados.Where(p => !codigosActuales.Contains(p))
            .Select(p => new Claim(Permisos.TipoClaimPermiso_wys, p)).ToList();

        if (aRetirar.Count == 0 && aConceder.Count == 0)
        {
            return null;
        }

        if (aRetirar.Count > 0)
        {
            await gestorUsuarios.RemoveClaimsAsync(usuario, aRetirar);
        }
        if (aConceder.Count > 0)
        {
            await gestorUsuarios.AddClaimsAsync(usuario, aConceder);
        }

        // La sesión abierta de esa persona caduca para que los permisos nuevos se apliquen ya
        await gestorUsuarios.UpdateSecurityStampAsync(usuario);

        foreach (var permiso in aConceder)
        {
            await registroSeguridad.RegistrarAsync(AccionesSeguridad.ConcesionPermiso_wys, $"Correcto ({permiso.Value}, afectado {usuario.Id})");
        }
        foreach (var permiso in aRetirar)
        {
            await registroSeguridad.RegistrarAsync(AccionesSeguridad.RetiradaPermiso_wys, $"Correcto ({permiso.Value}, afectado {usuario.Id})");
        }

        return null;
    }

    /// <summary>
    /// Comprueba en el servidor que los datos del formulario son válidos. Devuelve el error o null.
    /// </summary>
    private static string? ValidarDatos(string usuario, string nombreCompleto, string rol)
    {
        if (!FormatoUsuario().IsMatch(usuario))
        {
            return "El usuario debe tener entre 3 y 50 caracteres: letras minúsculas sin tildes, números, punto o guion.";
        }
        if (nombreCompleto.Length is < 1 or > 150)
        {
            return "El nombre completo es obligatorio (máximo 150 caracteres).";
        }
        if (!RolesDisponibles_wys.Contains(rol))
        {
            return "El rol elegido no es válido.";
        }
        return null;
    }

    /// <summary>
    /// Mira si el usuario de la ficha es la misma persona que está usando la aplicación.
    /// </summary>
    private async Task<bool> EsLaPersonaConectadaAsync(UsuarioAplicacion usuario)
    {
        return await usuarioActual.ObtenerIdAsync() == usuario.Id;
    }

    /// <summary>
    /// Cuenta cuántos administradores pueden entrar ahora mismo.
    /// </summary>
    private async Task<int> ContarAdministradoresActivosAsync()
    {
        return (await gestorUsuarios.GetUsersInRoleAsync(Permisos.RolAdministrador_wys)).Count(u => u.Activo);
    }

    /// <summary>
    /// Prepara la fila del listado a partir de los datos guardados.
    /// </summary>
    private static FichaUsuario CrearFicha(UsuarioAplicacion u, bool esAdministrador) =>
        new(u.Id, u.UserName ?? "", u.NombreCompleto,
            esAdministrador ? Permisos.RolAdministrador_wys : Permisos.RolUsuario_wys,
            u.Activo, u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow, u.UltimoAcceso);
}
