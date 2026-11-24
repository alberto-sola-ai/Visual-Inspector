namespace VisualInspector.Servicios.Planificacion;

/// <summary>
/// Trabajo en segundo plano que lee la hoja de planificación cada cierto tiempo (parámetro
/// MinutosLecturaPlanificacion, 15 por defecto), mientras la aplicación está en marcha, aunque
/// nadie tenga abierta la pantalla. La hoja se actualiza cada hora, así que no hace falta más.
/// </summary>
public sealed class ProcesoPlanificacion(
    ServicioPlanificacion planificacion,
    IServiceScopeFactory fabricaAmbitos,
    ILogger<ProcesoPlanificacion> registro) : BackgroundService
{
    /// <summary>
    /// Lee la hoja, espera el tiempo indicado y vuelve a empezar, como un despertador que suena
    /// cada 15 minutos. Se para cuando se cierra la aplicación.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        while (!parada.IsCancellationRequested)
        {
            await planificacion.ActualizarAsync(parada);

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(await LeerMinutosAsync()), parada);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Cada cuántos minutos se lee la hoja, según Parámetros. Si el valor no es válido o no se puede
    /// leer, se usa el de por defecto; y nunca menos de 5 ni más de 120 minutos.
    /// </summary>
    private async Task<int> LeerMinutosAsync()
    {
        try
        {
            await using var ambito = fabricaAmbitos.CreateAsyncScope();
            var parametros = ambito.ServiceProvider.GetRequiredService<ServicioParametros>();
            var valor = await parametros.ObtenerValorAsync(ServicioParametros.MinutosLecturaPlanificacion_wys,
                ServicioParametros.MinutosLecturaPlanificacionInicial_wys);
            return ServicioParametros.InterpretarMinutosLecturaPlanificacion(valor);
        }
        catch (Exception error)
        {
            registro.LogWarning(error, "Hoja de planificación: no se ha podido leer el intervalo de lectura; se usa el de por defecto.");
            return int.Parse(ServicioParametros.MinutosLecturaPlanificacionInicial_wys);
        }
    }
}
