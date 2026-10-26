using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace VisualInspector.Seguridad;

/// <summary>
/// Publicación detrás de un proxy (en producción, Nginx Proxy Manager, que pone el certificado).
/// El proxy le cuenta a la aplicación, en unas cabeceras, la IP real del usuario y si entró por
/// https. La aplicación solo se fía de esas cabeceras si vienen de los proxies indicados en la
/// configuración, como un portero que solo acepta recados del compañero de recepción: si las
/// manda cualquier otro equipo, se ignoran y nadie puede falsear su IP en el diario de seguridad.
/// </summary>
public static class ConfiguracionProxy
{
    public const string SeccionActivar_wys = "Seguridad:UsarCabecerasProxy";
    public const string SeccionProxies_wys = "Seguridad:ProxiesConfiables";

    /// <summary>
    /// Prepara qué cabeceras del proxy se leen y de quién se aceptan. Cada proxy de confianza es
    /// una IP (192.168.1.10) o una red entera (172.18.0.0/16). Las que no tengan buena forma se
    /// ignoran. Sin ninguna, solo se acepta un proxy en la misma máquina (localhost).
    /// </summary>
    public static void Configurar(ForwardedHeadersOptions opciones, IEnumerable<string?> proxiesConfiables)
    {
        // IP real del usuario y si entró por https
        opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Solo un salto: la aplicación está justo detrás del proxy
        opciones.ForwardLimit = 1;

        foreach (var texto in proxiesConfiables.Select(t => (t ?? "").Trim()).Where(t => t.Length > 0))
        {
            if (texto.Contains('/'))
            {
                if (System.Net.IPNetwork.TryParse(texto, out var red))
                {
                    opciones.KnownIPNetworks.Add(red);
                }
            }
            else if (IPAddress.TryParse(texto, out var ip))
            {
                opciones.KnownProxies.Add(ip);
            }
        }
    }
}
