using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisualInspector.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AnadeFinalizacionOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "OrdenesFabricacion",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "EnFabricacion",
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFin",
                table: "OrdenesFabricacion",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalizadaPor",
                table: "OrdenesFabricacion",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesFabricacion_Litografia",
                table: "OrdenesFabricacion",
                column: "Litografia");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrdenesFabricacion_Litografia",
                table: "OrdenesFabricacion");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "OrdenesFabricacion");

            migrationBuilder.DropColumn(
                name: "FechaFin",
                table: "OrdenesFabricacion");

            migrationBuilder.DropColumn(
                name: "FinalizadaPor",
                table: "OrdenesFabricacion");
        }
    }
}
