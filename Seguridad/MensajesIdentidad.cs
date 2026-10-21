using Microsoft.AspNetCore.Identity;

namespace VisualInspector.Seguridad;

/// <summary>
/// Traduce al castellano los avisos del sistema de usuarios de .NET (que vienen en inglés),
/// con frases cortas y sin tecnicismos.
/// </summary>
public sealed class MensajesIdentidad : IdentityErrorDescriber
{
    // Cada función de esta lista cambia un aviso concreto de inglés a castellano,
    // como un diccionario de bolsillo: un aviso por línea.
    public override IdentityError DefaultError() => Crear(nameof(DefaultError), "Algo no ha ido bien. Inténtalo de nuevo.");
    public override IdentityError ConcurrencyFailure() => Crear(nameof(ConcurrencyFailure), "Otra persona ha cambiado estos datos a la vez. Vuelve a cargar la página.");
    public override IdentityError PasswordMismatch() => Crear(nameof(PasswordMismatch), "La contraseña actual no es correcta.");
    public override IdentityError InvalidToken() => Crear(nameof(InvalidToken), "El enlace o código ya no es válido.");
    public override IdentityError InvalidUserName(string? userName) => Crear(nameof(InvalidUserName), "El nombre de usuario no es válido.");
    public override IdentityError DuplicateUserName(string userName) => Crear(nameof(DuplicateUserName), "Ya existe un usuario con ese nombre.");
    public override IdentityError DuplicateRoleName(string role) => Crear(nameof(DuplicateRoleName), "Ese rol ya existe.");
    public override IdentityError UserAlreadyHasPassword() => Crear(nameof(UserAlreadyHasPassword), "El usuario ya tiene contraseña.");
    public override IdentityError UserLockoutNotEnabled() => Crear(nameof(UserLockoutNotEnabled), "El bloqueo no está activado para este usuario.");
    public override IdentityError UserAlreadyInRole(string role) => Crear(nameof(UserAlreadyInRole), "El usuario ya tiene ese rol.");
    public override IdentityError UserNotInRole(string role) => Crear(nameof(UserNotInRole), "El usuario no tiene ese rol.");
    public override IdentityError PasswordTooShort(int length) => Crear(nameof(PasswordTooShort), $"La contraseña debe tener al menos {length} caracteres.");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Crear(nameof(PasswordRequiresUniqueChars), $"La contraseña debe tener al menos {uniqueChars} caracteres distintos.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => Crear(nameof(PasswordRequiresNonAlphanumeric), "La contraseña debe incluir algún símbolo.");
    public override IdentityError PasswordRequiresDigit() => Crear(nameof(PasswordRequiresDigit), "La contraseña debe incluir al menos un número.");
    public override IdentityError PasswordRequiresLower() => Crear(nameof(PasswordRequiresLower), "La contraseña debe incluir al menos una letra minúscula.");
    public override IdentityError PasswordRequiresUpper() => Crear(nameof(PasswordRequiresUpper), "La contraseña debe incluir al menos una letra mayúscula.");
    public override IdentityError RecoveryCodeRedemptionFailed() => Crear(nameof(RecoveryCodeRedemptionFailed), "El código de recuperación no es válido.");

    /// <summary>
    /// Prepara el aviso con su código interno y el texto en castellano que verá el usuario.
    /// </summary>
    private static IdentityError Crear(string codigo, string descripcion) => new() { Code = codigo, Description = descripcion };
}
