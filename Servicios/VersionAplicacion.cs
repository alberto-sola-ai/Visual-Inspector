using System.Reflection;

namespace VisualInspector.Servicios;

/// <summary>
/// La versión de la aplicación (por ejemplo "0.10.5"), leída del propio programa. Sale de la
/// etiqueta Version de VisualInspector.csproj, así que nunca hay que escribirla a mano en otro sitio.
/// </summary>
public static class VersionAplicacion
{
    // .NET añade a veces "+código" detrás de la versión (la marca del commit): se quita
    public static readonly string Numero_wys =
        (typeof(VersionAplicacion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "")
        .Split('+')[0];
}
