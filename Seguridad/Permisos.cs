namespace VisualInspector.Seguridad;

/// <summary>
/// Una llave concreta de la aplicación: su código interno, el área a la que pertenece
/// y una explicación sencilla para el administrador.
/// </summary>
public sealed record Permiso(string Codigo, string Area, string Descripcion);

/// <summary>
/// Lista de todos los permisos de la aplicación.
/// Es como la lista de llaves de una casa: cada llave abre una puerta concreta,
/// y el administrador decide qué llaves recibe cada persona.
/// Toda funcionalidad nueva añade aquí su permiso y lo exige en su página y en su servicio.
/// </summary>
public static class Permisos
{
    // Autocontroles visuales (núcleo de la aplicación, en desarrollo)
    public const string AutocontrolesVer_wys = "Autocontroles.Ver";
    public const string AutocontrolesRealizar_wys = "Autocontroles.Realizar";

    // Sección de Administración: solo el rol Administrador, no se concede a nadie más
    public const string Administracion_wys = "Administracion";

    // Nombres de los roles
    public const string RolAdministrador_wys = "Administrador";
    public const string RolUsuario_wys = "Usuario";

    // Tipos de "sello" (claim) que lleva la sesión de cada persona
    public const string TipoClaimPermiso_wys = "permiso";
    public const string TipoClaimDebeCambiarPassword_wys = "debe_cambiar_password";
    public const string TipoClaimNombreCompleto_wys = "nombre_completo";

    /// <summary>
    /// Los permisos que el administrador puede repartir, agrupados por área para la pantalla.
    /// </summary>
    public static readonly IReadOnlyList<Permiso> Catalogo_wys =
    [
        new(AutocontrolesVer_wys, "Autocontroles", "Ver la aplicación y consultar los autocontroles realizados."),
        new(AutocontrolesRealizar_wys, "Autocontroles", "Hacer un autocontrol nuevo: cargar patrón y muestras y comparar."),
    ];
}
