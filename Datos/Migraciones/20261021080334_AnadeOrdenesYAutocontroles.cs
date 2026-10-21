using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisualInspector.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AnadeOrdenesYAutocontroles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdenesFabricacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, collation: "utf8mb4_uca1400_spanish_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_uca1400_spanish_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreadoPor = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, collation: "utf8mb4_uca1400_spanish_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModificadoPor = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true, collation: "utf8mb4_uca1400_spanish_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaModificacion = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesFabricacion", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_uca1400_spanish_ai_ci");

            migrationBuilder.CreateTable(
                name: "Autocontroles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrdenFabricacionId = table.Column<int>(type: "int", nullable: false),
                    FechaControl = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    HorasDesdeInicio = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_uca1400_spanish_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Observaciones = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_uca1400_spanish_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreadoPor = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, collation: "utf8mb4_uca1400_spanish_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModificadoPor = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true, collation: "utf8mb4_uca1400_spanish_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaModificacion = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Autocontroles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Autocontroles_OrdenesFabricacion_OrdenFabricacionId",
                        column: x => x.OrdenFabricacionId,
                        principalTable: "OrdenesFabricacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_uca1400_spanish_ai_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Autocontroles_FechaControl",
                table: "Autocontroles",
                column: "FechaControl");

            migrationBuilder.CreateIndex(
                name: "IX_Autocontroles_OrdenFabricacionId_HorasDesdeInicio",
                table: "Autocontroles",
                columns: new[] { "OrdenFabricacionId", "HorasDesdeInicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesFabricacion_Codigo",
                table: "OrdenesFabricacion",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Autocontroles");

            migrationBuilder.DropTable(
                name: "OrdenesFabricacion");
        }
    }
}
