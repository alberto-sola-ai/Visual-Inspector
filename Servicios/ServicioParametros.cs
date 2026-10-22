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
        var error = Validar(parametro.Tipo, valor, parametro.Opciones);
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
        };

        var existentes = await datos.Parametros.Select(p => p.Clave).ToListAsync();
        var nuevos = iniciales.Where(p => !existentes.Contains(p.Clave)).ToList();
        if (nuevos.Count > 0)
        {
            datos.Parametros.AddRange(nuevos);
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
