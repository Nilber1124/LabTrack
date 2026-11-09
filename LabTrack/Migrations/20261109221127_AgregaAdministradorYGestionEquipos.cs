using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabTrack.Migrations
{
    /// <inheritdoc />
    public partial class AgregaAdministradorYGestionEquipos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeBaja",
                table: "Equipos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Equipos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Laboratorio",
                table: "Equipos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "IdRol", "Nombre" },
                values: new object[] { 3, "ADMINISTRADOR" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "IdRol",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "DeBaja",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "Laboratorio",
                table: "Equipos");
        }
    }
}
