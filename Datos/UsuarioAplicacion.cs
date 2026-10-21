using Microsoft.AspNetCore.Identity;

namespace VisualInspector.Datos;

/// <summary>
/// Una persona que puede entrar en la aplicación. Parte de la ficha estándar de .NET
/// (usuario y contraseña cifrada) y le añade los datos que pide la norma WYS.
/// </summary>
public class UsuarioAplicacion : IdentityUser, ITrazable
{
    // Nombre que se muestra en pantalla ("Ana García"); el usuario para entrar es UserName
    public string NombreCompleto { get; set; } = "";

    // Un usuario desactivado no puede entrar; se desactiva en lugar de borrarlo para no perder el rastro
    public bool Activo { get; set; } = true;

    // Obliga a poner una contraseña nueva en la próxima entrada (usuarios nuevos o contraseña restablecida)
    public bool DebeCambiarPassword { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    // Trazabilidad (la clase base es la de Identity, así que se añaden aquí directamente)
    public string CreadoPor { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
