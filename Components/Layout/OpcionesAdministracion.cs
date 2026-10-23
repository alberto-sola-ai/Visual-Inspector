namespace VisualInspector.Components.Layout;

/// <summary>
/// Una opción de la administración: dónde lleva, su icono corporativo y su explicación.
/// </summary>
public sealed record OpcionAdministracion(string Direccion, string Titulo, string Icono, string Texto);

/// <summary>
/// La lista de opciones de la administración. La usan el menú «Administración» de la cabecera y
/// la portada de Administración, para que siempre coincidan.
/// </summary>
public static class OpcionesAdministracion
{
    public static readonly OpcionAdministracion[] Lista_wys =
    [
        new("administracion/usuarios", "Usuarios y permisos", "chica",
            "Crear usuarios, activarlos o desactivarlos, desbloquearlos y darles permisos."),
        new("administracion/parametros", "Parámetros", "notas",
            "Ajustes de la aplicación que se cambian sin tocar el programa."),
        new("administracion/registros", "Registros Err./Seg.", "alerta",
            "Leer los diarios de errores y de seguridad para investigar lo que ha pasado."),
        new("administracion/versiones", "Versiones y actualizaciones", "ordenador",
            "Qué piezas usa la aplicación y si alguna necesita actualizarse."),
    ];
}
