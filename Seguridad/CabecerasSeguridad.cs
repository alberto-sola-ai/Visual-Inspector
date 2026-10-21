namespace VisualInspector.Seguridad;

/// <summary>
/// Añade a cada respuesta unas instrucciones de seguridad para el navegador, como los
/// carteles de "prohibido el paso" de una fábrica.
/// Ámbito Interno WYS: no se envía HSTS, porque el cifrado lo gestiona la red de la empresa.
/// </summary>
public sealed class CabecerasSeguridad(RequestDelegate siguiente)
{
    // Solo se cargan scripts, estilos, imágenes y fuentes de la propia aplicación.
    // 'unsafe-inline' en estilos lo necesita Blazor para algunos componentes.
    private const string PoliticaContenido_wys =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "font-src 'self'; connect-src 'self' ws: wss:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    /// <summary>
    /// Pone las cabeceras justo antes de enviar la respuesta y deja seguir la petición.
    /// </summary>
    public Task InvokeAsync(HttpContext contexto)
    {
        contexto.Response.OnStarting(() =>
        {
            var cabeceras = contexto.Response.Headers;
            cabeceras["Content-Security-Policy"] = PoliticaContenido_wys;
            cabeceras["X-Frame-Options"] = "DENY";                 // Nadie puede meter la aplicación dentro de otra web
            cabeceras["X-Content-Type-Options"] = "nosniff";       // El navegador no "adivina" el tipo de archivo
            cabeceras["Referrer-Policy"] = "strict-origin-when-cross-origin";
            return Task.CompletedTask;
        });

        return siguiente(contexto);
    }
}
