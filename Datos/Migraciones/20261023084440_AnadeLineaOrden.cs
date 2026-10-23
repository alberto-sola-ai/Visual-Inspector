using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisualInspector.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AnadeLineaOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Linea",
                table: "OrdenesFabricacion",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesFabricacion_Linea",
                table: "OrdenesFabricacion",
                column: "Linea");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrdenesFabricacion_Linea",
                table: "OrdenesFabricacion");

            migrationBuilder.DropColumn(
                name: "Linea",
                table: "OrdenesFabricacion");
        }
    }
}
