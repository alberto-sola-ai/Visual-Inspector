using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisualInspector.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class RenombraHorasANumeroControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HorasDesdeInicio",
                table: "Autocontroles",
                newName: "NumeroControl");

            migrationBuilder.RenameIndex(
                name: "IX_Autocontroles_OrdenFabricacionId_HorasDesdeInicio",
                table: "Autocontroles",
                newName: "IX_Autocontroles_OrdenFabricacionId_NumeroControl");

            // Los controles ya hechos pasan de horas fijas a número: 0 h → 1 (inicial), 4 h → 2,
            // 8 h → 3… Se hace en dos pasos (primero en negativo) para que el índice único no
            // choque a mitad de la conversión
            migrationBuilder.Sql("UPDATE Autocontroles SET NumeroControl = -(NumeroControl DIV 4 + 1);");
            migrationBuilder.Sql("UPDATE Autocontroles SET NumeroControl = -NumeroControl;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Vuelta atrás: número → horas fijas (1 → 0 h, 2 → 4 h…), también en dos pasos
            migrationBuilder.Sql("UPDATE Autocontroles SET NumeroControl = -((NumeroControl - 1) * 4) - 1;");
            migrationBuilder.Sql("UPDATE Autocontroles SET NumeroControl = -(NumeroControl + 1);");

            migrationBuilder.RenameColumn(
                name: "NumeroControl",
                table: "Autocontroles",
                newName: "HorasDesdeInicio");

            migrationBuilder.RenameIndex(
                name: "IX_Autocontroles_OrdenFabricacionId_NumeroControl",
                table: "Autocontroles",
                newName: "IX_Autocontroles_OrdenFabricacionId_HorasDesdeInicio");
        }
    }
}
