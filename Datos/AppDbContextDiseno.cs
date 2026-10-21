using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VisualInspector.Seguridad;

namespace VisualInspector.Datos;

/// <summary>
/// Solo la usa la herramienta de migraciones (dotnet ef) para dibujar las tablas sin
/// necesitar una base de datos encendida. La aplicación en marcha no pasa por aquí.
/// </summary>
public class AppDbContextDiseno : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Prepara una conexión "de mentira" con la versión de MariaDB de referencia (11.4 LTS).
    /// </summary>
    public AppDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=visualinspector;", new MariaDbServerVersion(new Version(11, 4)))
            .Options;

        return new AppDbContext(opciones, new UsuarioSistema());
    }

    /// <summary>
    /// Durante las migraciones no hay nadie conectado: responde siempre "sistema".
    /// </summary>
    private sealed class UsuarioSistema : IUsuarioActual
    {
        public Task<string> ObtenerNombreAsync() => Task.FromResult(UsuarioActual.NombreSistema_wys);
        public Task<string?> ObtenerIdAsync() => Task.FromResult<string?>(null);
    }
}
