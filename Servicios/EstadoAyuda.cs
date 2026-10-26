namespace VisualInspector.Servicios;

/// <summary>
/// Recuerda si el panel flotante de ayuda está abierto y qué se estaba buscando, como la nota
/// que dejas sobre la mesa mientras cambias de tarea. Hay una por persona conectada (por sesión
/// abierta), así que el panel sigue ahí al pasar de una pantalla a otra.
/// </summary>
public sealed class EstadoAyuda
{
    /// <summary>
    /// Avisa al panel y al menú de que algo ha cambiado para que se vuelvan a dibujar.
    /// </summary>
    public event Action? Cambio;

    public bool EstaAbierta { get; private set; }

    // Hasta que se abre por primera vez no se prepara el contenido, para no cargar la guía en balde
    public bool SeHaAbierto { get; private set; }

    public string Texto { get; private set; } = "";

    /// <summary>
    /// Abre el panel si está cerrado, y lo cierra si está abierto.
    /// </summary>
    public void Alternar()
    {
        EstaAbierta = !EstaAbierta;
        SeHaAbierto |= EstaAbierta;
        Cambio?.Invoke();
    }

    public void Cerrar()
    {
        if (!EstaAbierta) return;
        EstaAbierta = false;
        Cambio?.Invoke();
    }

    public void CambiarTexto(string? texto)
    {
        Texto = texto ?? "";
        Cambio?.Invoke();
    }
}
