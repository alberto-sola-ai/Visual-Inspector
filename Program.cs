// Punto de arranque de Visual Inspector: aquí se conectan todas las piezas de la aplicación
// (registros, idioma, base de datos, usuarios, permisos y pantallas) y se pone en marcha.
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;
using VisualInspector.Components;
using VisualInspector.Components.Account;
using VisualInspector.Datos;
using VisualInspector.Seguridad;
using VisualInspector.Servicios;

// Todo el programa usa el idioma y formatos de España (fechas dd/mm/aaaa, 1.234,56 €)
var culturaEspana = new CultureInfo("es-ES");
CultureInfo.DefaultThreadCurrentCulture = culturaEspana;
CultureInfo.DefaultThreadCurrentUICulture = culturaEspana;

var builder = WebApplication.CreateBuilder(args);

// Registros en log/: errores en err-log-AAAAMMDD.log y seguridad en Log-Wys-Seg-AAAAMMDD.log
// (la configuración está en appsettings.json, sección "Serilog")
builder.Host.UseSerilog((contexto, configuracion) =>
    configuracion.ReadFrom.Configuration(contexto.Configuration));

// --- Pantallas Blazor (renderizado en el servidor) ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddHttpContextAccessor();

// --- Base de datos MariaDB (conector Pomelo) ---
// La contraseña NO está en appsettings.json: en local va en user-secrets y en Docker en .env
var cadenaConexion = builder.Configuration.GetConnectionString("Principal")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Principal'.");
// AutoDetect pregunta al servidor su versión de MariaDB; se hace una sola vez y se recuerda
var versionServidor = new Lazy<ServerVersion>(() => ServerVersion.AutoDetect(cadenaConexion));
builder.Services.AddDbContextFactory<AppDbContext>(opciones =>
    opciones.UseMySql(cadenaConexion, versionServidor.Value), ServiceLifetime.Scoped);

// --- Usuarios y contraseñas (ASP.NET Core Identity) ---
// Contraseñas cifradas con Argon2id en lugar del cifrado de serie (norma SEGURIDAD WYS 2026)
builder.Services.AddScoped<IPasswordHasher<UsuarioAplicacion>, CifradoArgon2>();

builder.Services.AddAuthentication(opciones =>
    {
        opciones.DefaultScheme = IdentityConstants.ApplicationScheme;
        opciones.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddIdentityCore<UsuarioAplicacion>(opciones =>
    {
        // Sin correo: los usuarios los crea el administrador, nadie se registra por su cuenta
        opciones.SignIn.RequireConfirmedAccount = false;
        opciones.User.RequireUniqueEmail = false;

        // Bloqueo tras 3 intentos fallidos seguidos, durante 15 minutos
        opciones.Lockout.MaxFailedAccessAttempts = 3;
        opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        opciones.Lockout.AllowedForNewUsers = true;

        // Contraseñas: mínimo 8 caracteres con mayúscula, minúscula y número
        opciones.Password.RequiredLength = 8;
        opciones.Password.RequireUppercase = true;
        opciones.Password.RequireLowercase = true;
        opciones.Password.RequireDigit = true;
        opciones.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager<GestorEntrada>()
    .AddClaimsPrincipalFactory<FabricaSesionUsuario>()
    .AddErrorDescriber<MensajesIdentidad>()
    .AddDefaultTokenProviders();

// Cookie de sesión: caduca tras 30 minutos sin actividad y no es accesible desde JavaScript.
// Ámbito Interno WYS: sin la marca Secure obligatoria, porque la aplicación recibe HTTP
// (el cifrado lo gestiona la red de la empresa).
var minutosSesion = builder.Configuration.GetValue("Seguridad:MinutosInactividadSesion", 30);
builder.Services.ConfigureApplicationCookie(opciones =>
{
    opciones.ExpireTimeSpan = TimeSpan.FromMinutes(minutosSesion);
    opciones.SlidingExpiration = true;
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SameSite = SameSiteMode.Strict;
    opciones.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    opciones.LoginPath = "/Account/Login";
    opciones.AccessDeniedPath = "/Account/AccessDenied";
});

// Claves con las que .NET firma las sesiones y los formularios. Se guardan en la carpeta claves/
// (en Docker, un volumen) para que reiniciar o actualizar la aplicación no cierre las sesiones.
// Son secretos: la carpeta no se sube a GitHub ni entra en la imagen de Docker.
var carpetaClaves = Path.Combine(builder.Environment.ContentRootPath,
    builder.Configuration.GetValue("Seguridad:CarpetaClaves", "claves")!);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(carpetaClaves))
    .SetApplicationName("VisualInspector");

// Cada minuto se comprueba si la sesión sigue siendo válida (usuario desactivado, permisos cambiados…)
builder.Services.Configure<SecurityStampValidatorOptions>(opciones =>
    opciones.ValidationInterval = TimeSpan.FromMinutes(1));

// --- Permisos: denegación por defecto ---
// Todas las páginas exigen haber entrado; cada funcionalidad exige además su permiso
builder.Services.AddAuthorization(opciones =>
{
    opciones.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

    var codigos = Permisos.Catalogo_wys.Select(p => p.Codigo).Append(Permisos.Administracion_wys);
    foreach (var codigo in codigos)
    {
        opciones.AddPolicy(codigo, politica => politica
            .RequireAuthenticatedUser()
            .AddRequirements(new RequisitoPermiso(codigo)));
    }
});
builder.Services.AddSingleton<IAuthorizationHandler, ManejadorPermiso>();

// --- Servicios de la aplicación ---
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();
builder.Services.AddScoped<RegistroSeguridad>();
builder.Services.AddScoped<ServicioAutorizacion>();
builder.Services.AddScoped<ServicioParametros>();
builder.Services.AddScoped<ServicioUsuarios>();
builder.Services.AddScoped<ServicioVersiones>();

// Si la red de WYS publica la aplicación a través de un proxy, esto permite conocer la IP real
// del usuario para el diario de seguridad. Desactivado salvo que se active en la configuración.
var usarCabecerasProxy = builder.Configuration.GetValue("Seguridad:UsarCabecerasProxy", false);
if (usarCabecerasProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(opciones =>
        opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
}

var app = builder.Build();

if (usarCabecerasProxy)
{
    app.UseForwardedHeaders();
}

// Errores: en desarrollo se ve el detalle técnico; en pruebas y producción, solo un mensaje
// amable con un código (página /Error), y el detalle va a log/err-log
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Ámbito Interno WYS: sin UseHsts ni UseHttpsRedirection (la red de la empresa gestiona el cifrado)
app.UseMiddleware<CabecerasSeguridad>();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseRequestLocalization("es-ES");

// El informe de versiones solo se ve desde Administración, nunca como archivo suelto
app.Use(async (contexto, siguiente) =>
{
    if (contexto.Request.Path.StartsWithSegments("/" + ServicioVersiones.RutaInforme_wys, StringComparison.OrdinalIgnoreCase))
    {
        contexto.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await siguiente(contexto);
});

// Carpeta recursos/ (logo, fuentes, iconos y tema.css de la marca WYS), servida en /recursos
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "recursos")),
    RequestPath = "/recursos"
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Los archivos de la web (estilos y el programa de Blazor, _framework/blazor.web.js) se sirven
// sin exigir sesión: sin ellos la pantalla de entrada se ve mal y los botones no funcionan
app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Rutas adicionales del sistema de usuarios (cerrar sesión)
app.MapAdditionalIdentityEndpoints();

try
{
    // Prepara la base de datos (tablas, roles, parámetros y usuarios iniciales) antes de abrir
    await InicializadorDatos.InicializarAsync(app.Services);
    await app.RunAsync();
}
catch (Exception error)
{
    Log.Fatal(error, "La aplicación no ha podido arrancar.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
