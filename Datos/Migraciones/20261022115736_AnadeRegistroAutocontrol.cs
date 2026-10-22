using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisualInspector.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AnadeRegistroAutocontrol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Litografia",
                table: "OrdenesFabricacion",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "EsLitografiaDistintaConfirmada",
                table: "Autocontroles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRegistro",
                table: "Autocontroles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HuellaMuestra1",
                table: "Autocontroles",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "HuellaMuestra2",
                table: "Autocontroles",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "HuellaPatron",
                table: "Autocontroles",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Litografia",
                table: "Autocontroles",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NombreArchivoPatron",
                table: "Autocontroles",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RegistradoPor",
                table: "Autocontroles",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Resultado",
                table: "Autocontroles",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "SimilitudEntreMuestras",
                table: "Autocontroles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SimilitudMuestra1",
                table: "Autocontroles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SimilitudMuestra2",
                table: "Autocontroles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tolerancia",
                table: "Autocontroles",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VersionPatron",
                table: "Autocontroles",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true,
                collation: "utf8mb4_uca1400_spanish_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Diferencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AutocontrolId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_uca1400_spanish_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnMuestra1 = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EnMuestra2 = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    XMm = table.Column<decimal>(type: "decimal(7,1)", precision: 7, scale: 1, nullable: false),
                    YMm = table.Column<decimal>(type: "decimal(7,1)", precision: 7, scale: 1, nullable: false),
                    AnchoMm = table.Column<decimal>(type: "decimal(7,1)", precision: 7, scale: 1, nullable: false),
                    AltoMm = table.Column<decimal>(type: "decimal(7,1)", precision: 7, scale: 1, nullable: false),
                    AreaMm2 = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Evidencia = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Recorte = table.Column<byte[]>(type: "mediumblob", nullable: false),
                    Validacion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_uca1400_spanish_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Comentario = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_uca1400_spanish_ai_ci")
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
                    table.PrimaryKey("PK_Diferencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Diferencias_Autocontroles_AutocontrolId",
                        column: x => x.AutocontrolId,
                        principalTable: "Autocontroles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_uca1400_spanish_ai_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Diferencias_AutocontrolId_Numero",
                table: "Diferencias",
                columns: new[] { "AutocontrolId", "Numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Diferencias");

            migrationBuilder.DropColumn(
                name: "Litografia",
                table: "OrdenesFabricacion");

            migrationBuilder.DropColumn(
                name: "EsLitografiaDistintaConfirmada",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "FechaRegistro",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "HuellaMuestra1",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "HuellaMuestra2",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "HuellaPatron",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "Litografia",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "NombreArchivoPatron",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "RegistradoPor",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "Resultado",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "SimilitudEntreMuestras",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "SimilitudMuestra1",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "SimilitudMuestra2",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "Tolerancia",
                table: "Autocontroles");

            migrationBuilder.DropColumn(
                name: "VersionPatron",
                table: "Autocontroles");
        }
    }
}
