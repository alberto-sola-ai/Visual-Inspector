using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VisualInspector.Datos;
using VisualInspector.Seguridad;

namespace VisualInspector.Servicios;

/// <summary>
/// Lee y cambia los ajustes de la aplicación (tabla Parametros). Leer un ajuste concreto
/// puede hacerlo cualquier parte del programa; ver la lista y cambiarlos, solo el administrador.
/// </summary>
public sealed class ServicioParametros(
    IDbContextFactory<AppDbContext> fabricaDatos,
    ServicioAutorizacion autorizacion,
    RegistroSeguridad registroSeguridad)
{
    // Claves de los parámetros que se crean siempre (norma SEGURIDAD WYS 2026)
    public const string MfaActivo_wys = "MfaActivo";
    public const string IaActiva_wys = "IaActiva";
    public const string IaLimiteTokensActivo_wys = "IaLimiteTokensActivo";
    public const string IaRegistroConsumoActivo_wys = "IaRegistroConsumoActivo";

    // Tolerancia con la que se abre la comparación de cada autocontrol (el operario puede cambiarla)
    public const string ToleranciaPorDefecto_wys = "ToleranciaPorDefecto";

    // Cifras con las que empiezan habitualmente las órdenes de fabricación: los campos de la orden
    // vienen rellenos con ellas y se pueden cambiar (decisión de Alberto, 23/10/2026). Vacío = en blanco
    public const string PrefijoOrden_wys = "PrefijoOrden";
    public const string PrefijoOrdenInicial_wys = "10000";

    // Líneas de fabricación que se pueden elegir al crear un autocontrol, separadas por "|"
    // (decisión de Alberto, 23/10/2026). El administrador puede añadir más (por ejemplo L8)
    public const string Lineas_wys = "Lineas";
    public const string LineasIniciales_wys = "L1|L2|L3|L4|L5|L6|L7";
    public const int LongitudMaximaLinea_wys = 10;

    /// <summary>
    /// Convierte el texto del parámetro de líneas ("L1|L2|…") en la lista de líneas, sin huecos.
    /// </summary>
    public static IReadOnlyList<string> SepararLineas(string? valor) =>
        (valor ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Mira si la lista de líneas tiene buena forma: al menos una, sin repetidas y cada una con
    /// solo letras y cifras (como mucho 10). Devuelve el error o null.
    /// </summary>
    public static string? ValidarLineas(string valor)
    {
        var lineas = SepararLineas(valor);
        if (lineas.Count == 0)
            return "Indica al menos una línea, separadas por | (por ejemplo L1|L2).";
        if (lineas.Any(l => l.Length > LongitudMaximaLinea_wys || !l.All(char.IsAsciiLetterOrDigit)))
            return $"Cada línea solo puede tener letras y cifras, como mucho {LongitudMaximaLinea_wys}.";
        if (lineas.Distinct(StringComparer.OrdinalIgnoreCase).Count() != lineas.Count)
            return "Hay líneas repetidas.";
        return null;
    }

    // Hoja de planificación de producción (Google Sheets): si se usa para elegir la orden y cada
    // cuántos minutos se lee (decisión de Alberto, 24/11/2026: cada 15 minutos)
    public const string UsarPlanificacion_wys = "UsarPlanificacion";
    public const string MinutosLecturaPlanificacion_wys = "MinutosLecturaPlanificacion";
    public const string MinutosLecturaPlanificacionInicial_wys = "15";
    public const int MinutosLecturaPlanificacionMinimo_wys = 5;
    public const int MinutosLecturaPlanificacionMaximo_wys = 120;

    // Carpeta de patrones (PDF) tal como la ve la aplicación: en Docker, una carpeta del contenedor (por
    // ejemplo /app/patrones); si la aplicación corre en Windows, también una ruta de red. Vacío = la de la
    // configuración (docker-compose / .env). Decisión de Alberto, 24/11/2026
    public const string CarpetaPatrones_wys = "CarpetaPatrones";

    /// <summary>
    /// Mira si la carpeta de patrones tiene buena forma: vacía (se usa la de la configuración) o una ruta
    /// completa (que empiece por / o por una letra de unidad o \\), sin caracteres raros. Devuelve el error o null.
    /// </summary>
    public static string? ValidarCarpetaPatrones(string valor)
    {
        if (valor.Length == 0)
            return null;
        if (valor.Any(char.IsControl) || valor.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return "La ruta tiene caracteres no válidos.";
        // En Docker (Linux) la aplicación no puede abrir rutas de red de Windows: se dice claro qué hacer
        if (!OperatingSystem.IsWindows() && (valor.StartsWith(@"\\") || (valor.Length > 1 && valor[1] == ':')))
            return "La aplicación funciona en Docker y no puede abrir rutas de Windows (\\\\servidor\\… o C:\\…). " +
                "La carpeta de red la monta IT en el servidor; aquí se escribe la carpeta tal como la ve la aplicación, " +
                "por ejemplo /app/patrones. Si no sabes cuál es, déjalo vacío.";
        if (!Path.IsPathFullyQualified(valor) && !valor.StartsWith('/'))
            return OperatingSystem.IsWindows()
                ? "Escribe la ruta completa, por ejemplo \\\\servidor\\carpeta o C:\\patrones."
                : "Escribe la ruta completa tal como la ve la aplicación, por ejemplo /app/patrones.";
        return null;
    }

    /// <summary>
    /// Convierte el valor del parámetro MinutosLecturaPlanificacion en minutos: si no es un número
    /// entero, el de por defecto (15); y siempre entre 5 y 120.
    /// </summary>
    public static int InterpretarMinutosLecturaPlanificacion(string? valor) =>
        int.TryParse(valor, out var minutos)
            ? Math.Clamp(minutos, MinutosLecturaPlanificacionMinimo_wys, MinutosLecturaPlanificacionMaximo_wys)
            : int.Parse(MinutosLecturaPlanificacionInicial_wys);

    public const string ValorSi_wys = "Si";
    public const string ValorNo_wys = "No";

    private const string MotivoSinIa_wys = "No disponible: la aplicación todavía no usa inteligencia artificial.";

    /// <summary>
    /// Mira si un interruptor Sí/No está encendido. Si el parámetro no existe, se considera apagado.
    /// </summary>
    public async Task<bool> EstaActivoAsync(string clave)
    {
        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var valor = await datos.Parametros
            .Where(p => p.Clave == clave)
            .Select(p => p.Valor)
            .FirstOrDefaultAsync();
        return valor == ValorSi_wys;
    }

    /// <summary>
    /// Lee el valor de un parámetro, o el valor indicado si no existe.
    /// </summary>
    public async Task<string> ObtenerValorAsync(string clave, string valorSiNoExiste)
    {
        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var valor = await datos.Parametros
            .Where(p => p.Clave == clave)
            .Select(p => p.Valor)
            .FirstOrDefaultAsync();
        return valor ?? valorSiNoExiste;
    }

    /// <summary>
    /// Devuelve todos los parámetros para la pantalla de Administración (solo administradores).
    /// </summary>
    public async Task<List<Parametro>> ObtenerTodosAsync()
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        return await datos.Parametros.AsNoTracking().OrderBy(p => p.Clave).ToListAsync();
    }

    /// <summary>
    /// Cambia el valor de un parámetro (solo administradores). Comprueba en el servidor que el
    /// valor tiene el formato correcto y apunta el cambio en el diario de seguridad.
    /// Devuelve un mensaje de error para el usuario, o null si todo ha ido bien.
    /// </summary>
    public async Task<string?> GuardarAsync(int id, string? nuevoValor)
    {
        await autorizacion.ExigirPermisoAsync(Permisos.Administracion_wys);

        await using var datos = await fabricaDatos.CreateDbContextAsync();
        var parametro = await datos.Parametros.FindAsync(id);
        if (parametro is null)
        {
            return "El parámetro ya no existe.";
        }

        if (!parametro.EsEditable)
        {
            return parametro.MotivoNoEditable ?? "Este parámetro no se puede cambiar.";
        }

        // Zero Trust: el valor se valida aquí aunque la pantalla ya lo haya hecho
        var valor = (nuevoValor ?? "").Trim();
        var error = Validar(parametro.Tipo, valor, parametro.Opciones)
            // El inicio de las órdenes: solo cifras, y menos de 10 (la orden entera tiene 10)
            ?? (parametro.Clave == PrefijoOrden_wys && (valor.Length > 9 || !valor.All(char.IsAsciiDigit))
                ? "El inicio de las órdenes solo puede tener cifras, como mucho 9 (la orden completa tiene 10)."
                : null)
            // Las líneas de fabricación: al menos una, sin repetidas, solo letras y cifras
            ?? (parametro.Clave == Lineas_wys ? ValidarLineas(valor) : null)
            // Lectura de la hoja de planificación: minutos enteros, ni tan seguido que sature a Google
            // ni tan espaciado que la lista se quede vieja
            ?? (parametro.Clave == MinutosLecturaPlanificacion_wys
                && (!int.TryParse(valor, out var minutos) || minutos < MinutosLecturaPlanificacionMinimo_wys || minutos > MinutosLecturaPlanificacionMaximo_wys)
                ? $"Indica un número entero de minutos entre {MinutosLecturaPlanificacionMinimo_wys} y {MinutosLecturaPlanificacionMaximo_wys}."
                : null)
            // Carpeta de patrones: vacía o una ruta completa
            ?? (parametro.Clave == CarpetaPatrones_wys ? ValidarCarpetaPatrones(valor) : null);
        if (parametro.Clave == Lineas_wys && error is null)
        {
            // Se guardan siempre igual de limpias: "L1|L2|L3"
            valor = string.Join('|', SepararLineas(valor));
        }
        if (error is not null)
        {
            return error;
        }

        if (parametro.Valor == valor)
        {
            return null;
        }

        var valorAnterior = parametro.Valor;
        parametro.Valor = valor;
        await datos.SaveChangesAsync();

        await registroSeguridad.RegistrarAsync(AccionesSeguridad.CambioParametro_wys,
            $"Correcto ({parametro.Clave}: {valorAnterior} -> {valor})");
        return null;
    }

    /// <summary>
    /// Crea los parámetros que deben existir siempre, si aún no están. Se llama al arrancar.
    /// </summary>
    public static async Task CrearInicialesAsync(AppDbContext datos)
    {
        var iniciales = new List<Parametro>
        {
            new() { Clave = MfaActivo_wys, Valor = ValorNo_wys, Tipo = TipoParametro.SiNo,
                Descripcion = "Permite a los usuarios activar la verificación en dos pasos (un segundo candado con una aplicación del móvil)." },
            new() { Clave = IaActiva_wys, Valor = ValorNo_wys, Tipo = TipoParametro.SiNo,
                Descripcion = "Activa las funciones con inteligencia artificial.",
                EsEditable = false, MotivoNoEditable = MotivoSinIa_wys },
            new() { Clave = IaLimiteTokensActivo_wys, Valor = ValorNo_wys, Tipo = TipoParametro.SiNo,
                Descripcion = "Aplica los límites de uso de la IA. Actívalo solo después de comprobar que la API de IA usada ofrece esta información de forma sencilla. Consulta doc/seguridad.md, apartado IA.",
                EsEditable = false, MotivoNoEditable = MotivoSinIa_wys },
            new() { Clave = IaRegistroConsumoActivo_wys, Valor = ValorNo_wys, Tipo = TipoParametro.SiNo,
                Descripcion = "Guarda el consumo de la IA y muestra su pantalla. Actívalo solo después de comprobar que la API de IA usada ofrece esta información de forma sencilla. Consulta doc/seguridad.md, apartado IA.",
                EsEditable = false, MotivoNoEditable = MotivoSinIa_wys },
            new() { Clave = ToleranciaPorDefecto_wys, Valor = "Normal", Tipo = TipoParametro.Lista, Opciones = "Estricta|Normal|Amplia",
                Descripcion = "Tolerancia con la que se abre la comparación de cada autocontrol. Estricta: avisa de más diferencias, también pequeñas o suaves. Normal: equilibrio para un autocontrol rápido. Amplia: solo las diferencias más claras. El operario puede cambiarla en cada autocontrol." },
            new() { Clave = PrefijoOrden_wys, Valor = PrefijoOrdenInicial_wys, Tipo = TipoParametro.Texto,
                Descripcion = "Cifras con las que empiezan habitualmente las órdenes de fabricación. Los campos de la orden (nuevo autocontrol y buscadores) vienen rellenos con ellas y se pueden cambiar. Vacío: salen en blanco." },
            new() { Clave = Lineas_wys, Valor = LineasIniciales_wys, Tipo = TipoParametro.Texto,
                Descripcion = "Líneas de fabricación que se pueden elegir al crear un autocontrol, separadas por | (por ejemplo L1|L2|L3). Solo letras y cifras, sin repetir." },
            new() { Clave = UsarPlanificacion_wys, Valor = ValorSi_wys, Tipo = TipoParametro.SiNo,
                Descripcion = "Muestra en «Nuevo autocontrol» las órdenes de la hoja de planificación de producción (Google Sheets) para elegirlas con un clic. Solo funciona si está configurada la cuenta de Google (ver doc/planificacion-google.md). La orden se puede seguir escribiendo a mano." },
            new() { Clave = MinutosLecturaPlanificacion_wys, Valor = MinutosLecturaPlanificacionInicial_wys, Tipo = TipoParametro.Numero,
                Descripcion = $"Cada cuántos minutos se lee la hoja de planificación de producción (entre {MinutosLecturaPlanificacionMinimo_wys} y {MinutosLecturaPlanificacionMaximo_wys}). El cambio se aplica a partir de la siguiente lectura." },
            new() { Clave = CarpetaPatrones_wys, Valor = "", Tipo = TipoParametro.Texto,
                Descripcion = "Carpeta donde se buscan los patrones (PDF) por su especialidad, tal como la ve la aplicación. En Docker es una carpeta del contenedor (por ejemplo /app/patrones), no una ruta de red de Windows: la carpeta de red la monta IT en el servidor. Vacío: se usa la de la instalación (.env). Ver doc/patrones.md." },
        };

        var existentes = await datos.Parametros.ToListAsync();
        var nuevos = iniciales.Where(p => existentes.All(e => e.Clave != p.Clave)).ToList();
        datos.Parametros.AddRange(nuevos);

        // Las descripciones las escribe el equipo (el administrador solo cambia el valor): si se han mejorado
        // en el código, se ponen al día también en los parámetros que ya existían. El valor no se toca
        foreach (var existente in existentes)
        {
            var inicial = iniciales.FirstOrDefault(p => p.Clave == existente.Clave);
            if (inicial is not null && existente.Descripcion != inicial.Descripcion)
            {
                existente.Descripcion = inicial.Descripcion;
            }
        }

        if (datos.ChangeTracker.HasChanges())
        {
            await datos.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Comprueba que el valor encaja con el tipo del parámetro. Devuelve el error o null.
    /// </summary>
    private static string? Validar(TipoParametro tipo, string valor, string? opciones)
    {
        if (tipo == TipoParametro.Lista)
        {
            var permitidos = (opciones ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return permitidos.Contains(valor) ? null : $"El valor debe ser uno de estos: {string.Join(", ", permitidos)}.";
        }

        if (valor.Length > 500)
        {
            return "El valor es demasiado largo (máximo 500 caracteres).";
        }

        return tipo switch
        {
            TipoParametro.SiNo when valor is not (ValorSi_wys or ValorNo_wys) => "El valor debe ser Sí o No.",
            TipoParametro.Numero when !decimal.TryParse(valor, NumberStyles.Number, CultureInfo.GetCultureInfo("es-ES"), out _)
                => "El valor debe ser un número (por ejemplo 1.234,56).",
            TipoParametro.Fecha when !DateTime.TryParseExact(valor, "dd/MM/yyyy", CultureInfo.GetCultureInfo("es-ES"), DateTimeStyles.None, out _)
                => "La fecha debe tener el formato dd/mm/aaaa.",
            _ => null
        };
    }
}
