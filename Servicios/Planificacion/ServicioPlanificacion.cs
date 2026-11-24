namespace VisualInspector.Servicios.Planificacion;

/// <summary>
/// Guarda la última copia de la hoja de planificación de producción (Google Sheets) para que las
/// pantallas la consulten al instante, sin esperar a Google. La copia la refresca cada cierto tiempo
/// el proceso en segundo plano (<see cref="ProcesoPlanificacion"/>) o el botón «Actualizar ahora».
/// Una sola instancia para toda la aplicación. No comprueba permisos: eso lo hace quien la usa
/// desde una pantalla (<see cref="ServicioAutocontroles.ObtenerPlanificacionAsync"/>).
/// </summary>
public sealed class ServicioPlanificacion(
    IConfiguration configuracion,
    IWebHostEnvironment entorno,
    IServiceScopeFactory fabricaAmbitos,
    ILogger<ServicioPlanificacion> registro)
{
    // Ajustes de appsettings.json (en Docker, variables del .env). Ninguno es secreto: el secreto
    // es el archivo JSON de la carpeta de credenciales, que no se sube a GitHub
    public const string SeccionIdHoja_wys = "Planificacion:IdHoja";
    public const string SeccionPestana_wys = "Planificacion:Pestana";
    public const string SeccionCarpetaCredenciales_wys = "Planificacion:CarpetaCredenciales";
    public const string CarpetaCredencialesInicial_wys = "wys-api-sheet";

    // «Actualizar ahora» no vuelve a preguntar a Google si se acaba de hacer: así varios operarios
    // pulsándolo a la vez no saturan la cuenta de Google
    private static readonly TimeSpan EsperaEntreLecturasManuales_wys = TimeSpan.FromMinutes(1);

    // Solo una lectura a la vez
    private readonly SemaphoreSlim _cerrojo = new(1, 1);
    private EstadoPlanificacion _estado = EstadoPlanificacion.Inactiva_wys;
    private DateTime _ultimoIntento = DateTime.MinValue;
    private bool _avisoConfiguracionDado;

    /// <summary>
    /// Lo último que se sabe de la hoja (se cambia de golpe, nunca a medias).
    /// </summary>
    public EstadoPlanificacion Estado => Volatile.Read(ref _estado);

    /// <summary>
    /// Pide la hoja a Google si ha pasado al menos un minuto desde la última vez, para el botón
    /// «Actualizar ahora». Si no, se queda con la copia que ya hay.
    /// </summary>
    public async Task ActualizarSiHaceFaltaAsync(CancellationToken cancelacion = default)
    {
        if (DateTime.Now - _ultimoIntento < EsperaEntreLecturasManuales_wys)
        {
            return;
        }
        await ActualizarAsync(cancelacion);
    }

    /// <summary>
    /// Lee la hoja de Google y guarda la copia nueva. Si algo falla (sin internet, Google no
    /// contesta, la hoja cambió de forma), se queda con la copia anterior, la marca como no
    /// actualizada y lo apunta en log/err-log, sin romper nada.
    /// </summary>
    public async Task ActualizarAsync(CancellationToken cancelacion = default)
    {
        // Si ya hay una lectura en marcha, no se lanza otra: la que está en curso sirve para todos
        if (!await _cerrojo.WaitAsync(0, cancelacion))
        {
            return;
        }

        // Solo se marca un fallo de lectura si de verdad se llegó a pedir la hoja: un fallo antes (por
        // ejemplo, al leer los ajustes) no debe hacer parecer activa una planificación apagada
        var seLlegoALeer = false;
        try
        {
            _ultimoIntento = DateTime.Now;

            await using var ambito = fabricaAmbitos.CreateAsyncScope();
            var parametros = ambito.ServiceProvider.GetRequiredService<ServicioParametros>();

            var idHoja = (configuracion[SeccionIdHoja_wys] ?? "").Trim();
            var pestana = (configuracion[SeccionPestana_wys] ?? "").Trim();
            var rutaClave = BuscarClave();
            var estaEncendida = await parametros.EstaActivoAsync(ServicioParametros.UsarPlanificacion_wys);
            if (!estaEncendida || idHoja.Length == 0 || pestana.Length == 0 || rutaClave is null)
            {
                // Sin configurar o apagada: la pantalla sigue como siempre, con la orden a mano
                if (estaEncendida && idHoja.Length > 0 && !_avisoConfiguracionDado)
                {
                    registro.LogWarning("Hoja de planificación: falta la clave de la cuenta de servicio (un único archivo .json en la carpeta {Carpeta}).",
                        configuracion[SeccionCarpetaCredenciales_wys] ?? CarpetaCredencialesInicial_wys);
                    _avisoConfiguracionDado = true;
                }
                Volatile.Write(ref _estado, EstadoPlanificacion.Inactiva_wys);
                return;
            }

            var lineas = ServicioParametros.SepararLineas(
                await parametros.ObtenerValorAsync(ServicioParametros.Lineas_wys, ServicioParametros.LineasIniciales_wys));
            var prefijo = (await parametros.ObtenerValorAsync(ServicioParametros.PrefijoOrden_wys, ServicioParametros.PrefijoOrdenInicial_wys)).Trim();

            seLlegoALeer = true;
            var filas = await LectorHojaGoogle.LeerAsync(rutaClave, idHoja, pestana, cancelacion);
            var resultado = ConversorPlanificacion.Convertir(filas, lineas, prefijo);
            if (!resultado.TieneCabecera)
            {
                registro.LogWarning("Hoja de planificación: no se encuentran las columnas LINEA, PRIORIDAD y OF en la pestaña {Pestana}.", pestana);
                MarcarFallo();
                return;
            }
            // Una hoja sin ninguna orden válida no es una planificación de verdad (suele ser que producción
            // la está recargando justo ahora): se sigue con la copia anterior en lugar de dar por terminadas
            // todas las órdenes abiertas
            if (resultado.Ordenes.Count == 0)
            {
                registro.LogWarning("Hoja de planificación: se ha leído sin ninguna orden válida ({Descartadas} filas descartadas). Se sigue con la copia anterior.",
                    resultado.FilasDescartadas);
                MarcarFallo();
                return;
            }
            if (resultado.FilasDescartadas > 0)
            {
                registro.LogWarning("Hoja de planificación: {Descartadas} filas no se han podido usar (línea que no existe en la aplicación, orden que no tiene 10 cifras, sin prioridad u orden repetida).",
                    resultado.FilasDescartadas);
            }

            Volatile.Write(ref _estado, new EstadoPlanificacion(true, resultado.Ordenes, DateTime.Now, false, resultado.FechaCargaHoja));
            // Solo sale en la consola (docker compose logs app), no en log/: sirve para comprobar que funciona
            registro.LogInformation("Hoja de planificación leída: {Ordenes} órdenes.", resultado.Ordenes.Count);
        }
        catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
        {
            // La aplicación se está cerrando: no es un error
        }
        catch (Exception error)
        {
            registro.LogWarning(error, "Hoja de planificación: no se ha podido leer. Se sigue mostrando la copia anterior.");
            if (seLlegoALeer)
            {
                MarcarFallo();
            }
        }
        finally
        {
            _cerrojo.Release();
        }
    }

    /// <summary>
    /// Deja la copia anterior tal cual, pero marcada como no actualizada para avisar en pantalla.
    /// </summary>
    private void MarcarFallo()
    {
        var anterior = Estado;
        Volatile.Write(ref _estado, anterior with { EstaActiva = true, UltimaLecturaFallida = true });
    }

    /// <summary>
    /// Busca el archivo de la clave en la carpeta de credenciales. Tiene que haber uno solo: si hay
    /// varios, no se adivina cuál es el bueno. Devuelve null si no lo encuentra.
    /// </summary>
    private string? BuscarClave()
    {
        var carpeta = Path.Combine(entorno.ContentRootPath,
            configuracion[SeccionCarpetaCredenciales_wys] ?? CarpetaCredencialesInicial_wys);
        if (!Directory.Exists(carpeta))
        {
            return null;
        }
        var archivos = Directory.GetFiles(carpeta, "*.json");
        return archivos.Length == 1 ? archivos[0] : null;
    }
}
