namespace VisualInspector.Seguridad;

/// <summary>
/// Acciones de seguridad que se apuntan en el diario Log-Wys-Seg.
/// </summary>
public static class AccionesSeguridad
{
    public const string InicioSesion_wys = "InicioSesion";
    public const string CierreSesion_wys = "CierreSesion";
    public const string Bloqueo_wys = "Bloqueo";
    public const string Desbloqueo_wys = "Desbloqueo";
    public const string CambioPassword_wys = "CambioPassword";
    public const string RestablecerPassword_wys = "RestablecerPassword";
    public const string AltaUsuario_wys = "AltaUsuario";
    public const string ModificacionUsuario_wys = "ModificacionUsuario";
    public const string ActivacionUsuario_wys = "ActivacionUsuario";
    public const string DesactivacionUsuario_wys = "DesactivacionUsuario";
    public const string CambioRol_wys = "CambioRol";
    public const string ConcesionPermiso_wys = "ConcesionPermiso";
    public const string RetiradaPermiso_wys = "RetiradaPermiso";
    public const string AccesoDenegado_wys = "AccesoDenegado";
    public const string CambioParametro_wys = "CambioParametro";
    public const string ActivacionMfa_wys = "ActivacionMfa";
    public const string DesactivacionMfa_wys = "DesactivacionMfa";
    public const string ReaperturaOrden_wys = "ReaperturaOrden";
    public const string CierreAutomaticoOrden_wys = "CierreAutomaticoOrden";
    public const string AnulacionAutocontrol_wys = "AnulacionAutocontrol";
    public const string CorreccionImpacto_wys = "CorreccionImpacto";
    public const string EliminacionAutocontrol_wys = "EliminacionAutocontrol";
    public const string LimpiezaDatos_wys = "LimpiezaDatos";
    public const string ConsultaRegistro_wys = "ConsultaRegistro";
    public const string TokenFormularioNoValido_wys = "TokenFormularioNoValido";
}

/// <summary>
/// El diario de seguridad: apunta quién hizo qué, desde dónde, cuándo y cómo salió,
/// como el libro de visitas de la portería. Nunca apunta contraseñas ni tokens.
/// </summary>
public sealed class RegistroSeguridad(
    ILogger<RegistroSeguridad> registro,
    IUsuarioActual usuarioActual,
    IHttpContextAccessor accesoPeticion)
{
    // La categoría "Seguridad" hace que Serilog lo escriba en log/Log-Wys-Seg-AAAAMMDD.log
    public const string Categoria_wys = "Seguridad";

    /// <summary>
    /// Apunta un evento de seguridad. Si no se indica de quién es, se usa la persona conectada.
    /// </summary>
    /// <param name="accion">Qué ha pasado (ver AccionesSeguridad).</param>
    /// <param name="resultado">Cómo ha salido: Correcto, Fallido, Denegado… con un detalle breve.</param>
    /// <param name="idUsuario">ID del usuario afectado, si no es la persona conectada.</param>
    public async Task RegistrarAsync(string accion, string resultado, string? idUsuario = null)
    {
        idUsuario ??= await usuarioActual.ObtenerIdAsync();
        var ip = accesoPeticion.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

        using (registro.BeginScope(new Dictionary<string, object> { ["Categoria"] = Categoria_wys }))
        {
            // Nivel Information: así el evento va solo a Log-Wys-Seg y no al diario de errores
            registro.LogInformation(
                "Usuario: {UsuarioId} | IP: {Ip} | Accion: {Accion} | Resultado: {Resultado}",
                idUsuario ?? "-", ip, accion, resultado);
        }
    }
}
