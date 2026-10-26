using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VisualInspector.Datos;
using VisualInspector.Seguridad;

namespace VisualInspector.Servicios;

/// <summary>
/// Una orden en la lista de limpieza, con lo que se borraría junto a ella.
/// </summary>
public sealed record FilaLimpieza(
    int Id, string Codigo, string? Litografia, EstadoOrden Estado, string CreadoPor, DateTime FechaCreacion,
    int Autocontroles, int Diferencias);

/// <summary>
/// Lo que se ha borrado, para enseñarlo al terminar.
/// </summary>
public sealed record ResultadoLimpieza(int Ordenes, int Autocontroles, int Diferencias);

/// <summary>
/// Limpieza de datos de prueba para la sección de Administración: borra órdenes de fabricación
/// completas (con sus autocontroles y diferencias), como vaciar la papelera de una vez. Es una
/// excepción a "los autocontroles no se borran", pensada para dejar la base de datos limpia al
/// pasar de las pruebas a lo real (decisión de Alberto, 26/10/2026).
/// Solo el administrador, que además debe volver a escribir su contraseña; el servidor lo comprueba
/// y todo intento queda apuntado en el diario de seguridad. Una contraseña incorrecta no bloquea la cuenta. Los métodos que cambian algo devuelven
/// un mensaje de error para el usuario, o null si va bien.
/// </summary>
public sealed class ServicioLimpieza(
    IDbContextFactory<AppDbContext> fabricaDatos,
    UserManager<UsuarioAplicacion> gestorUsuarios,
    ServicioAutorizacion autorizacion,
    IUsuarioActual usuarioActual,
    RegistroSeguridad registroSeguridad)
{
    // Para que la pantalla y el diario no se desborden: como mucho 200 órdenes de una vez
    public const int MaximoOrdenes_wys = 200;

    // Cuántos códigos de orden se copian en el diario de seguridad
    private const int MaximoCodigosDiario_wys = 20;

    // Tras 3 contraseñas incorrectas seguidas, 38 segundos de pausa (decisión de Alberto, 26/10/2026)
    public const int FallosAntesDePausa_wys = 3;
    public const int SegundosPausa_wys = 38;

    // Fallos seguidos y fin de la pausa de cada usuario. Es estático porque el servicio se crea de
    // nuevo en cada sesión y la cuenta debe seguir aunque el administrador cierre y abra la pantalla
    private static readonly Dictionary<string, (int Fallos, DateTime? PausaHasta)> Intentos_wys = [];

    /// <summary>
    /// Segundos que le faltan al usuario para poder volver a probar (0 si no está en pausa).
    /// </summary>
    private static int SegundosDePausa(string idUsuario)
    {
        lock (Intentos_wys)
        {
            if (Intentos_wys.TryGetValue(idUsuario, out var intento) && intento.PausaHasta is { } hasta && hasta > DateTime.Now)
                return (int)Math.Ceiling((hasta - DateTime.Now).TotalSeconds);
            return 0;
        }
    }

    /// <summary>
    /// Suma un fallo. Devuelve true si con este fallo empieza la pausa (y la cuenta vuelve a cero).
    /// </summary>
    private static bool AnotarFallo(string idUsuario)
    {
        lock (Intentos_wys)
        {
            var fallos = (Intentos_wys.TryGetValue(idUsuario, out var intento) ? intento.Fallos : 0) + 1;
            if (fallos >= FallosAntesDePausa_wys)
            {
                Intentos_wys[idUsuario] = (0, DateTime.Now.AddSeconds(SegundosPausa_wys));
                return true;
            }
            Intentos_wys[idUsuario] = (fallos, null);
            return false;
        }
    }

    private static void BorrarFallos(string idUsuario)
    {
        lock (Intentos_wys)
        {
            Intentos_wys.Remove(idUsuario);
        }
    }

    /// <summary>
    /// Devuelve las órdenes creadas entre las dos fechas (ambas incluidas; vacía = sin límite),
    /// de la más reciente a la más antigua.
    /// </summary>
    public async Task<List<FilaLimpieza>> ListarAsync(DateOnly? desde, DateOnly? hasta)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var consulta = datos.OrdenesFabricacion.AsNoTracking();
        if (desde is { } d) consulta = consulta.Where(o => o.FechaCreacion >= d.ToDateTime(TimeOnly.MinValue));
        // "Hasta" incluye todo ese día: se compara con el inicio del día siguiente
        if (hasta is { } h) consulta = consulta.Where(o => o.FechaCreacion < h.AddDays(1).ToDateTime(TimeOnly.MinValue));

        return await consulta
            .OrderByDescending(o => o.FechaCreacion)
            .Take(MaximoOrdenes_wys)
            .Select(o => new FilaLimpieza(o.Id, o.Codigo, o.Litografia, o.Estado, o.CreadoPor, o.FechaCreacion,
                o.Autocontroles.Count, o.Autocontroles.SelectMany(a => a.Diferencias).Count()))
            .ToListAsync();
    }

    /// <summary>
    /// Comprueba la contraseña del administrador conectado, sin borrar nada. Un fallo no bloquea
    /// la cuenta: se puede volver a intentar, y queda apuntado en el diario de seguridad. Tras 3
    /// fallos seguidos hay una pausa de 38 segundos.
    /// </summary>
    public async Task<string?> VerificarPasswordAsync(string? password)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);
        return await ComprobarPasswordAsync(password);
    }

    /// <summary>
    /// Borra las órdenes elegidas con todos sus autocontroles y diferencias, en una sola operación
    /// (o se borra todo o no se borra nada). Vuelve a comprobar la contraseña: aunque alguien se
    /// saltara la pantalla, el servidor no borra sin ella.
    /// </summary>
    public async Task<(string? Error, ResultadoLimpieza? Resultado)> EliminarOrdenesAsync(
        IReadOnlyCollection<int> idsOrdenes, string? password)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        var ids = idsOrdenes.Distinct().ToList();
        if (ids.Count == 0)
            return ("Elige al menos una orden.", null);
        if (ids.Count > MaximoOrdenes_wys)
            return ($"Como máximo se pueden borrar {MaximoOrdenes_wys} órdenes de una vez.", null);

        var errorPassword = await ComprobarPasswordAsync(password);
        if (errorPassword is not null)
            return (errorPassword, null);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var ordenes = await datos.OrdenesFabricacion.AsNoTracking()
            .Where(o => ids.Contains(o.Id)).Select(o => o.Codigo).ToListAsync();
        if (ordenes.Count == 0)
            return ("Las órdenes elegidas ya no existen.", null);

        await using var transaccion = await datos.Database.BeginTransactionAsync();
        // Orden de borrado: primero las diferencias, luego los autocontroles y al final las órdenes
        var idsAutocontroles = await datos.Autocontroles
            .Where(a => ids.Contains(a.OrdenFabricacionId)).Select(a => a.Id).ToListAsync();
        var diferencias = await datos.Diferencias
            .Where(d => idsAutocontroles.Contains(d.AutocontrolId)).ExecuteDeleteAsync();
        var autocontroles = await datos.Autocontroles
            .Where(a => idsAutocontroles.Contains(a.Id)).ExecuteDeleteAsync();
        var borradas = await datos.OrdenesFabricacion
            .Where(o => ids.Contains(o.Id)).ExecuteDeleteAsync();
        await transaccion.CommitAsync();

        // En el diario van los códigos (los primeros) y las cifras, nunca contraseñas
        var codigos = string.Join(", ", ordenes.OrderBy(c => c).Take(MaximoCodigosDiario_wys));
        var resto = ordenes.Count > MaximoCodigosDiario_wys ? $" y {ordenes.Count - MaximoCodigosDiario_wys} más" : "";
        await registroSeguridad.RegistrarAsync(AccionesSeguridad.LimpiezaDatos_wys,
            $"Correcto: {borradas} orden(es) [{codigos}{resto}], {autocontroles} autocontrol(es), {diferencias} diferencia(s)");

        return (null, new ResultadoLimpieza(borradas, autocontroles, diferencias));
    }

    /// <summary>
    /// Mira si la contraseña escrita es la de la persona conectada. Devuelve un mensaje de error
    /// para el usuario, o null si es correcta. Apunta en el diario cada fallo.
    /// </summary>
    private async Task<string?> ComprobarPasswordAsync(string? password)
    {
        var idUsuario = await usuarioActual.ObtenerIdAsync();
        var usuario = idUsuario is null ? null : await gestorUsuarios.FindByIdAsync(idUsuario);
        if (usuario is null)
            return "No se ha podido comprobar tu usuario. Vuelve a entrar.";

        // En pausa tras varios fallos: ni se mira la contraseña hasta que pase el tiempo
        var segundosPausa = SegundosDePausa(usuario.Id);
        if (segundosPausa > 0)
            return $"Demasiados intentos. Espera {segundosPausa} segundos para volver a intentarlo.";

        // Un fallo no bloquea la cuenta (decisión de Alberto): se avisa, se apunta en el diario y,
        // tras 3 fallos seguidos, se obliga a esperar unos segundos
        if (string.IsNullOrEmpty(password) || !await gestorUsuarios.CheckPasswordAsync(usuario, password))
        {
            var empiezaPausa = AnotarFallo(usuario.Id);
            await registroSeguridad.RegistrarAsync(AccionesSeguridad.LimpiezaDatos_wys,
                empiezaPausa ? "Fallido: contraseña incorrecta (pausa)" : "Fallido: contraseña incorrecta");
            return empiezaPausa
                ? $"Contraseña incorrecta. Has fallado {FallosAntesDePausa_wys} veces: espera {SegundosPausa_wys} segundos."
                : "Contraseña incorrecta.";
        }

        BorrarFallos(usuario.Id);
        return null;
    }
}
