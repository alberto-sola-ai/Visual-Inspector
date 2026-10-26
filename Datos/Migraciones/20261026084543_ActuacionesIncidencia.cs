using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisualInspector.Datos.Migraciones
{
    /// <summary>
    /// Añade a cada diferencia la columna con lo que se hizo en producción ante una incidencia
    /// (parada de línea, retención de producto…). Vacía en lo ya registrado.
    /// </summary>
    public partial class ActuacionesIncidencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Actuaciones",
                table: "Diferencias",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Actuaciones",
                table: "Diferencias");
        }
    }
}
