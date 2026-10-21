using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VisualInspector.Seguridad;
using VisualInspector.Servicios;

namespace VisualInspector.Datos;

/// <summary>
/// Prepara la base de datos al arrancar: crea o actualiza las tablas, los roles, los
/// parámetros y los usuarios iniciales (admin y, si se pide, prueba). Si ya existen, no los toca.
/// </summary>
public static class InicializadorDatos
{
    /// <summary>
    /// Deja la base de datos lista para usarse, como quien abre la tienda por la mañana.
    /// </summary>
    public static async Task InicializarAsync(IServiceProvider servicios)
    {
        using var ambito = servicios.CreateScope();
        var proveedor = ambito.ServiceProvider;
        var configuracion = proveedor.GetRequiredService<IConfiguration>();

        // Aplica las migraciones pendientes: así las tablas siempre están al día con el código
        var datos = proveedor.GetRequiredService<AppDbContext>();
        await datos.Database.MigrateAsync();

        var gestorRoles = proveedor.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var rol in ServicioUsuarios.RolesDisponibles_wys)
        {
            if (!await gestorRoles.RoleExistsAsync(rol))
            {
                await gestorRoles.CreateAsync(new IdentityRole(rol));
            }
        }

        await ServicioParametros.CrearInicialesAsync(datos);

        var gestorUsuarios = proveedor.GetRequiredService<UserManager<UsuarioAplicacion>>();
        await CrearUsuarioInicialAsync(gestorUsuarios, "admin", "Administrador", Permisos.RolAdministrador_wys,
            configuracion["UsuariosIniciales:PasswordAdmin"]);

        if (configuracion.GetValue<bool>("UsuariosIniciales:CrearUsuarioPrueba"))
        {
            // El usuario de prueba nace sin ningún permiso: el administrador se los concede
            await CrearUsuarioInicialAsync(gestorUsuarios, "prueba", "Usuario de prueba", Permisos.RolUsuario_wys,
                configuracion["UsuariosIniciales:PasswordPrueba"]);
        }
    }

    /// <summary>
    /// Crea un usuario inicial si no existe, con cambio de contraseña obligatorio al entrar.
    /// La contraseña sale de la configuración (user-secrets o .env); si no hay, se inventa una
    /// y se enseña una sola vez en la consola, nunca en los ficheros de log/.
    /// </summary>
    private static async Task CrearUsuarioInicialAsync(UserManager<UsuarioAplicacion> gestorUsuarios,
        string usuario, string nombreCompleto, string rol, string? passwordConfigurada)
    {
        if (await gestorUsuarios.FindByNameAsync(usuario) is not null)
        {
            return;
        }

        var esGenerada = string.IsNullOrWhiteSpace(passwordConfigurada);
        var password = esGenerada ? GenerarPassword() : passwordConfigurada!;

        var nuevo = new UsuarioAplicacion
        {
            UserName = usuario,
            NombreCompleto = nombreCompleto,
            Activo = true,
            DebeCambiarPassword = true
        };

        var resultado = await gestorUsuarios.CreateAsync(nuevo, password);
        if (!resultado.Succeeded)
        {
            // Sin el usuario inicial no se puede administrar la aplicación: se detiene el arranque
            throw new InvalidOperationException(
                $"No se pudo crear el usuario inicial '{usuario}': {string.Join(" ", resultado.Errors.Select(e => e.Description))}");
        }

        await gestorUsuarios.AddToRoleAsync(nuevo, rol);

        if (esGenerada)
        {
            // Console.WriteLine y no el registro: así la contraseña nunca llega a los ficheros de log/
            Console.WriteLine();
            Console.WriteLine("==================================================================");
            Console.WriteLine($" Usuario inicial creado: {usuario}");
            Console.WriteLine($" Contraseña temporal:    {password}");
            Console.WriteLine(" Se muestra SOLO esta vez. Se pedirá cambiarla al entrar.");
            Console.WriteLine("==================================================================");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Inventa una contraseña de 16 caracteres que cumple las reglas (mayúscula, minúscula y número).
    /// </summary>
    private static string GenerarPassword()
    {
        const string Mayusculas_wys = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string Minusculas_wys = "abcdefghijkmnopqrstuvwxyz";
        const string Numeros_wys = "23456789";
        const string Todos_wys = Mayusculas_wys + Minusculas_wys + Numeros_wys;

        var caracteres = new char[16];
        caracteres[0] = Mayusculas_wys[RandomNumberGenerator.GetInt32(Mayusculas_wys.Length)];
        caracteres[1] = Minusculas_wys[RandomNumberGenerator.GetInt32(Minusculas_wys.Length)];
        caracteres[2] = Numeros_wys[RandomNumberGenerator.GetInt32(Numeros_wys.Length)];
        for (var i = 3; i < caracteres.Length; i++)
        {
            caracteres[i] = Todos_wys[RandomNumberGenerator.GetInt32(Todos_wys.Length)];
        }

        // Se barajan para que la mayúscula no esté siempre al principio
        RandomNumberGenerator.Shuffle(caracteres.AsSpan());
        return new string(caracteres);
    }
}
