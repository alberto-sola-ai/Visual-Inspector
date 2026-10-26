using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisualInspector.Datos.Migraciones
{
    /// <summary>
    /// Añade a cada diferencia los minutos de parada y los tubos retenidos (aproximados, los apunta el
    /// operario). Vacíos en lo ya registrado.
    /// </summary>
    public partial class ImpactoProductivoIncidencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinutosParada",
                table: "Diferencias",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TubosRetenidos",
                table: "Diferencias",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinutosParada",
                table: "Diferencias");

            migrationBuilder.DropColumn(
                name: "TubosRetenidos",
                table: "Diferencias");
        }
    }
}
