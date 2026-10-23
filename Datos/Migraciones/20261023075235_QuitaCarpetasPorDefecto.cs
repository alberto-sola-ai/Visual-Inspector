using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisualInspector.Datos.Migraciones
{
    /// <summary>
    /// Borra los parámetros de las carpetas por defecto (versión 0.11.0), que se quitaron en la
    /// 0.11.5: el navegador no deja abrir «Elegir archivo» en una carpeta indicada por la
    /// aplicación, así que no tenían uso (decisión de Alberto, 23/10/2026).
    /// </summary>
    public partial class QuitaCarpetasPorDefecto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Parametros WHERE Clave IN ('CarpetaPatrones', 'CarpetaMuestras');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se vuelven a crear: la aplicación ya no tiene esa funcionalidad
        }
    }
}
