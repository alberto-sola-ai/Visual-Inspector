using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VisualInspector.Datos;
using VisualInspector.Seguridad;

namespace VisualInspector.Servicios;

/// <summary>
/// Una fila del listado de autocontroles.
/// </summary>
public sealed record FilaAutocontrol(
    int Id, string CodigoOrden, DateTime FechaControl, int HorasDesdeInicio,
    EstadoAutocontrol Estado, string CreadoPor);

/// <summary>
/// Todo lo que se ve en la ficha de un autocontrol, incluido quién y cuándo lo creó o cambió.
/// </summary>
public sealed record FichaAutocontrol(
    int Id, string CodigoOrden, DateTime FechaControl, int HorasDesdeInicio,
    EstadoAutocontrol Estado, string? Observaciones,
    string CreadoPor, DateTime FechaCreacion, string? ModificadoPor, DateTime? FechaModificacion);

/// <summary>
/// Resultado de crear un autocontrol: su número si ha ido bien, o el error para el usuario.
/// </summary>
public sealed record ResultadoCreacion(int? Id, string? Error);

/// <summary>
/// Gestiona los autocontroles: crearlos, listarlos y ver su ficha.
/// Ver exige el permiso Autocontroles.Ver y crear exige Autocontroles.Realizar,
/// comprobados aquí en el servidor aunque la pantalla ya lo haya hecho.
/// </summary>
public sealed partial class ServicioAutocontroles(
    IDbContextFactory<AppDbContext> fabricaDatos,
    ServicioAutorizacion autorizacion)
{
    // Cada cuántas horas se repite el autocontrol durante una orden de fabricación
    public const int HorasEntreControles_wys = 4;
    public const int LongitudMaximaObservaciones_wys = 500;
    private const int MaximoFilasListado_wys = 200;

    // Código de orden de fabricación: exactamente 10 cifras (por ejemplo 1000063961)
    [GeneratedRegex("^[0-9]{10}$")]
    private static partial Regex FormatoOrden();

    /// <summary>
    /// Dice cómo se llama un control según sus horas: "Inicial", "+4 h", "+8 h"…
    /// </summary>
    public static string NombreTipo(int horasDesdeInicio) =>
        horasDesdeInicio == 0 ? "Inicial" : $"+{horasDesdeInicio} h";

    /// <summary>
    /// Explica el estado en palabras sencillas para la pantalla.
    /// </summary>
    public static string NombreEstado(EstadoAutocontrol estado) => estado switch
    {
        EstadoAutocontrol.PendienteArchivos => "Pendiente de archivos",
        _ => estado.ToString()
    };

    /// <summary>
    /// Devuelve los últimos autocontroles, del más reciente al más antiguo.
    /// Si se indica un código de orden (o parte de él), solo los de esas órdenes.
    /// </summary>
    public async Task<List<FilaAutocontrol>> ListarAsync(string? filtroOrden)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesVer_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var consulta = datos.Autocontroles.AsNoTracking();

        // Solo se aceptan cifras en el filtro: cualquier otra cosa se ignora (Zero Trust)
        var filtro = new string((filtroOrden ?? "").Where(char.IsAsciiDigit).ToArray());
        if (filtro.Length > 0)
        {
            consulta = consulta.Where(a => a.OrdenFabricacion.Codigo.StartsWith(filtro));
        }

        return await consulta
            .OrderByDescending(a => a.FechaControl)
            .Take(MaximoFilasListado_wys)
            .Select(a => new FilaAutocontrol(a.Id, a.OrdenFabricacion.Codigo, a.FechaControl,
                a.HorasDesdeInicio, a.Estado, a.CreadoPor))
            .ToListAsync();
    }

    /// <summary>
    /// Devuelve la ficha de un autocontrol, o null si no existe.
    /// </summary>
    public async Task<FichaAutocontrol?> ObtenerAsync(int id)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesVer_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        return await datos.Autocontroles.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new FichaAutocontrol(a.Id, a.OrdenFabricacion.Codigo, a.FechaControl,
                a.HorasDesdeInicio, a.Estado, a.Observaciones,
                a.CreadoPor, a.FechaCreacion, a.ModificadoPor, a.FechaModificacion))
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Dice qué tipo de control tocaría ahora para una orden ("Inicial" si es nueva,
    /// "+4 h" si ya tiene el inicial…), para enseñarlo antes de crearlo.
    /// Devuelve null si el código no es válido.
    /// </summary>
    public async Task<string?> PrevisualizarTipoAsync(string? codigoOrden)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);

        var codigo = (codigoOrden ?? "").Trim();
        if (!FormatoOrden().IsMatch(codigo))
        {
            return null;
        }

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var cantidad = await datos.Autocontroles.CountAsync(a => a.OrdenFabricacion.Codigo == codigo);
        return NombreTipo(cantidad * HorasEntreControles_wys);
    }

    /// <summary>
    /// Crea un autocontrol nuevo para una orden de fabricación, como quien abre una hoja nueva
    /// en el cuaderno de la orden. Si la orden aún no existe, la crea. La fecha y hora las pone
    /// el servidor, y el tipo (Inicial, +4 h, +8 h…) se calcula solo.
    /// </summary>
    public async Task<ResultadoCreacion> CrearAsync(string? codigoOrden, string? observaciones)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.AutocontrolesRealizar_wys);

        // Zero Trust: todo se vuelve a validar aquí, aunque la pantalla ya lo haya hecho
        var codigo = (codigoOrden ?? "").Trim();
        if (!FormatoOrden().IsMatch(codigo))
        {
            return new(null, "La orden de fabricación debe tener exactamente 10 cifras, solo números.");
        }

        var textoObservaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        if (textoObservaciones?.Length > LongitudMaximaObservaciones_wys)
        {
            return new(null, $"Las observaciones no pueden pasar de {LongitudMaximaObservaciones_wys} caracteres.");
        }

        // Si dos personas crean a la vez un control de la misma orden, la base de datos rechaza
        // el duplicado (índice único) y se vuelve a intentar con el tipo siguiente
        for (var intento = 1; intento <= 3; intento++)
        {
            await using var datos = await fabricaDatos.CreateDbContextAsync();
            try
            {
                var orden = await datos.OrdenesFabricacion.FirstOrDefaultAsync(o => o.Codigo == codigo);
                if (orden is null)
                {
                    orden = new OrdenFabricacion { Codigo = codigo };
                    datos.OrdenesFabricacion.Add(orden);
                }
                else if (!orden.Activa)
                {
                    return new(null, "Esa orden de fabricación está desactivada.");
                }

                var cantidad = orden.Id == 0 ? 0 : await datos.Autocontroles.CountAsync(a => a.OrdenFabricacionId == orden.Id);
                var autocontrol = new Autocontrol
                {
                    OrdenFabricacion = orden,
                    FechaControl = DateTime.Now,
                    HorasDesdeInicio = cantidad * HorasEntreControles_wys,
                    Estado = EstadoAutocontrol.PendienteArchivos,
                    Observaciones = textoObservaciones
                };
                datos.Autocontroles.Add(autocontrol);
                await datos.SaveChangesAsync();
                return new(autocontrol.Id, null);
            }
            catch (DbUpdateException)
            {
                // Otro autocontrol u orden se ha guardado justo a la vez: se repite con datos frescos
            }
        }

        return new(null, "No se ha podido guardar porque otra persona estaba creando el mismo autocontrol. Inténtalo de nuevo.");
    }
}
