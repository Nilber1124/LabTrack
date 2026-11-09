using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabTrack.Migrations
{
    /// <inheritdoc />
    public partial class m1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Estudiante"),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Activo"),
                    IntentosFallidos = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    BloqueadoHasta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                    table.CheckConstraint("CK_usuarios_Estado", "[Estado] IN ('Activo', 'Penalizado')");
                    table.CheckConstraint("CK_usuarios_IntentosFallidos", "[IntentosFallidos] >= 0");
                    table.CheckConstraint("CK_usuarios_Rol", "[Rol] IN ('Estudiante', 'Encargado')");
                });

            migrationBuilder.CreateTable(
                name: "equipos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoInventario = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CategoriaId = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_equipos_categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reservas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstudianteId = table.Column<long>(type: "bigint", nullable: false),
                    EquipoId = table.Column<long>(type: "bigint", nullable: false),
                    Inicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Fin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ResueltaPor = table.Column<long>(type: "bigint", nullable: true),
                    ResueltaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DevueltaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservas", x => x.Id);
                    table.CheckConstraint("CK_reservas_Estado", "[Estado] IN ('Pendiente', 'Aprobada', 'Rechazada', 'Devuelta')");
                    table.CheckConstraint("CK_reservas_Periodo", "[Fin] > [Inicio]");
                    table.ForeignKey(
                        name: "FK_reservas_equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "equipos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reservas_usuarios_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reservas_usuarios_ResueltaPor",
                        column: x => x.ResueltaPor,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "penalizaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservaId = table.Column<long>(type: "bigint", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Levantada_En = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_penalizaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_penalizaciones_reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categorias_Nombre",
                table: "categorias",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipos_CategoriaId",
                table: "equipos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_equipos_CodigoInventario",
                table: "equipos",
                column: "CodigoInventario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_penalizaciones_ReservaId",
                table: "penalizaciones",
                column: "ReservaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reservas_EquipoId_Estado_Inicio_Fin",
                table: "reservas",
                columns: new[] { "EquipoId", "Estado", "Inicio", "Fin" });

            migrationBuilder.CreateIndex(
                name: "IX_reservas_EstudianteId",
                table: "reservas",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_reservas_ResueltaPor",
                table: "reservas",
                column: "ResueltaPor");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Codigo",
                table: "usuarios",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "penalizaciones");

            migrationBuilder.DropTable(
                name: "reservas");

            migrationBuilder.DropTable(
                name: "equipos");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "categorias");
        }
    }
}
