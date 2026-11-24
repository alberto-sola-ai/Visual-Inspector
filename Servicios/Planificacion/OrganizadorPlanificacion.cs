using VisualInspector.Datos;

namespace VisualInspector.Servicios.Planificacion;

/// <summary>
/// Lo que se enseña de una línea en «Nuevo Autocontrol».
/// </summary>
/// <param name="EnCurso">La orden cargada ahora en la línea (prioridad 1), o null si la hoja no la trae.</param>
/// <param name="DatosEnCurso">Lo que la aplicación sabe de la orden en curso, o null si aún no existe en ella.</param>
/// <param name="PorCerrar">Órdenes de esta línea que siguen abiertas en la aplicación y no son la de curso:
/// lo normal es que ya hayan terminado y haya que finalizarlas.</param>
/// <param name="Siguientes">El resto de órdenes de la hoja para esta línea, por prioridad (van en el desplegable).</param>
public sealed record ColumnaPlanificacion(
    string Linea, OrdenPlanificada? EnCurso, ResumenOrdenApp? DatosEnCurso,
    IReadOnlyList<ResumenOrdenApp> PorCerrar, IReadOnlyList<OrdenPlanificada> Siguientes);

/// <summary>
/// Reparte la planificación por líneas para la pantalla: en cada línea, la orden en curso, las
/// abiertas que faltan por cerrar y, aparte, las que vienen detrás (decisión de Alberto,
/// 24/11/2026). No habla con Google ni con la base de datos, así se puede probar con datos de ejemplo.
/// </summary>
public static class OrganizadorPlanificacion
{
    /// <summary>
    /// Prepara una columna por línea, en el orden del parámetro Lineas, como quien ordena las
    /// fichas de cada máquina en su casillero. Las líneas sin nada que enseñar no salen.
    /// </summary>
    /// <param name="ordenes">Órdenes de la hoja, ya ordenadas por línea y prioridad.</param>
    /// <param name="enApp">Lo que la aplicación sabe de las órdenes abiertas y de las que están en curso.</param>
    /// <param name="lineas">Líneas de la aplicación (parámetro Lineas).</param>
    public static IReadOnlyList<ColumnaPlanificacion> Organizar(
        IReadOnlyList<OrdenPlanificada> ordenes, IReadOnlyList<ResumenOrdenApp> enApp, IReadOnlyList<string> lineas)
    {
        var porCodigo = enApp.ToDictionary(o => o.Codigo);
        // Una orden en curso en cualquier línea nunca se propone para cerrar
        var codigosEnCurso = ordenes.Where(o => o.EstaEnCurso).Select(o => o.Codigo).ToHashSet();

        var columnas = new List<ColumnaPlanificacion>();
        foreach (var linea in lineas)
        {
            var deLinea = ordenes.Where(o => string.Equals(o.Linea, linea, StringComparison.OrdinalIgnoreCase)).ToList();
            var enCurso = deLinea.FirstOrDefault(o => o.EstaEnCurso);

            // Abiertas en la aplicación en esta línea, con algún autocontrol: la más reciente primero
            var porCerrar = enApp
                .Where(o => o.Estado == EstadoOrden.EnFabricacion && o.Autocontroles > 0
                    && string.Equals(o.Linea, linea, StringComparison.OrdinalIgnoreCase)
                    && !codigosEnCurso.Contains(o.Codigo))
                .OrderByDescending(o => o.UltimoControl)
                .ToList();
            var codigosPorCerrar = porCerrar.Select(o => o.Codigo).ToHashSet();

            // Las que ya salen como abiertas no se repiten en el desplegable
            var siguientes = deLinea.Where(o => o != enCurso && !codigosPorCerrar.Contains(o.Codigo)).ToList();

            if (enCurso is null && porCerrar.Count == 0 && siguientes.Count == 0)
            {
                continue;
            }
            columnas.Add(new ColumnaPlanificacion(linea, enCurso,
                enCurso is null ? null : porCodigo.GetValueOrDefault(enCurso.Codigo), porCerrar, siguientes));
        }
        return columnas;
    }
}
