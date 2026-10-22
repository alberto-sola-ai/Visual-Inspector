using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisualInspector.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AnadeOpcionesParametros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Opciones",
                table: "Parametros",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Opciones",
                table: "Parametros");
        }
    }
}
