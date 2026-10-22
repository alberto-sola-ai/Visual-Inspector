using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisualInspector.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AnadeAnulacionAutocontrol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnuladoPor",
                table: "Autocontroles",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "EstaAnulado",
                table: "Autocontroles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAnulacion",
                table: "Autocontroles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "Autocontroles",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnuladoPor",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "EstaAnulado",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "FechaAnulacion",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                table: "Autocontroles");
        }
    }
}
