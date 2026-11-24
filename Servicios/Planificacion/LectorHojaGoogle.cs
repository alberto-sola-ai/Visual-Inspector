using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace VisualInspector.Servicios.Planificacion;

/// <summary>
/// Habla con Google Sheets para traer las celdas de la hoja de planificación. Entra con la cuenta
/// de servicio (una «cuenta robot» de Google con su clave en un archivo JSON) y solo pide permiso
/// de lectura: aunque alguien robara la clave, no podría cambiar la hoja.
/// </summary>
public static class LectorHojaGoogle
{
    // Si Google tarda más que esto, se deja para la próxima vez en lugar de quedarse esperando
    private static readonly TimeSpan TiempoMaximoEspera_wys = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Trae todas las celdas de la pestaña indicada, fila a fila, como quien fotocopia la hoja.
    /// Los números llegan como números (sin el formato de la hoja), para no confundir «1.234» con decimales.
    /// </summary>
    /// <param name="rutaClave">Archivo JSON con la clave de la cuenta de servicio.</param>
    /// <param name="idHoja">Identificador del documento (lo que va en la dirección tras /d/).</param>
    /// <param name="pestana">Nombre de la pestaña (por ejemplo tubosFabricados).</param>
    public static async Task<IList<IList<object>>> LeerAsync(string rutaClave, string idHoja, string pestana, CancellationToken cancelacion)
    {
        var credencial = CredentialFactory.FromFile<ServiceAccountCredential>(rutaClave)
            .ToGoogleCredential()
            .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

        using var servicio = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credencial,
            ApplicationName = "Visual Inspector"
        });
        servicio.HttpClient.Timeout = TiempoMaximoEspera_wys;

        // Las comillas simples permiten nombres de pestaña con espacios
        var peticion = servicio.Spreadsheets.Values.Get(idHoja, $"'{pestana.Replace("'", "''")}'");
        peticion.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;
        var respuesta = await peticion.ExecuteAsync(cancelacion);
        return respuesta.Values ?? [];
    }
}
