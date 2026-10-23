using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using VisualInspector.Datos;

namespace VisualInspector.Seguridad;

/// <summary>
/// Guarda las contraseñas convertidas en un "revoltijo" Argon2id que no se puede deshacer.
/// Sustituye al cifrado de serie de .NET (PBKDF2), que no está en la lista permitida por la
/// norma SEGURIDAD WYS 2026.
/// </summary>
public sealed class CifradoArgon2 : IPasswordHasher<UsuarioAplicacion>
{
    // Parámetros mínimos recomendados por OWASP para Argon2id
    private const int MemoriaKb_wys = 19456;   // 19 MiB
    private const int Iteraciones_wys = 2;
    private const int Hilos_wys = 1;
    private const int BytesSal_wys = 16;
    private const int BytesResultado_wys = 32;
    private const string Prefijo_wys = "argon2id";

    /// <summary>
    /// Convierte una contraseña en su revoltijo, con una "sal" distinta cada vez para que dos
    /// contraseñas iguales nunca den el mismo resultado.
    /// Formato guardado: argon2id$m=19456,t=2,p=1$sal$resultado
    /// </summary>
    public string HashPassword(UsuarioAplicacion user, string password)
    {
        var sal = RandomNumberGenerator.GetBytes(BytesSal_wys);
        var resultado = Calcular(password, sal, MemoriaKb_wys, Iteraciones_wys, Hilos_wys, BytesResultado_wys);

        // Los parámetros se guardan junto al resultado para poder endurecerlos en el futuro
        return $"{Prefijo_wys}$m={MemoriaKb_wys},t={Iteraciones_wys},p={Hilos_wys}$" +
               $"{Convert.ToBase64String(sal)}${Convert.ToBase64String(resultado)}";
    }

    /// <summary>
    /// Comprueba si la contraseña escrita es la correcta: la vuelve a revolver con la misma sal
    /// y mira si sale lo mismo. Si se guardó con parámetros más débiles que los actuales,
    /// pide que se vuelva a guardar.
    /// </summary>
    public PasswordVerificationResult VerifyHashedPassword(UsuarioAplicacion user, string hashedPassword, string providedPassword)
    {
        // Una contraseña vacía nunca es correcta: se responde "no" sin llegar a calcular nada
        // (el cálculo Argon2id no admite contraseñas vacías y daría un error interno)
        if (string.IsNullOrEmpty(providedPassword) || string.IsNullOrEmpty(hashedPassword))
        {
            return PasswordVerificationResult.Failed;
        }

        var partes = hashedPassword.Split('$');
        if (partes.Length != 4 || partes[0] != Prefijo_wys)
        {
            return PasswordVerificationResult.Failed;
        }

        // Lee "m=19456,t=2,p=1"
        var parametros = partes[1].Split(',')
            .Select(p => p.Split('='))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => int.TryParse(p[1], out var valor) ? valor : 0);

        if (!parametros.TryGetValue("m", out var memoria) || !parametros.TryGetValue("t", out var iteraciones)
            || !parametros.TryGetValue("p", out var hilos) || memoria <= 0 || iteraciones <= 0 || hilos <= 0)
        {
            return PasswordVerificationResult.Failed;
        }

        byte[] sal, esperado;
        try
        {
            sal = Convert.FromBase64String(partes[2]);
            esperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }

        var calculado = Calcular(providedPassword, sal, memoria, iteraciones, hilos, esperado.Length);

        // Comparación en tiempo constante: no da pistas a quien intente adivinar midiendo tiempos
        if (!CryptographicOperations.FixedTimeEquals(calculado, esperado))
        {
            return PasswordVerificationResult.Failed;
        }

        var esMasDebil = memoria < MemoriaKb_wys || iteraciones < Iteraciones_wys || hilos < Hilos_wys;
        return esMasDebil ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Success;
    }

    /// <summary>
    /// Hace el revoltijo Argon2id con los parámetros indicados.
    /// </summary>
    private static byte[] Calcular(string password, byte[] sal, int memoria, int iteraciones, int hilos, int bytesResultado)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = sal,
            MemorySize = memoria,
            Iterations = iteraciones,
            DegreeOfParallelism = hilos
        };
        return argon2.GetBytes(bytesResultado);
    }
}
