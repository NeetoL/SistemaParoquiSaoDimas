using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SaoDimas.Infraestrutura.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class ComunidadesEDizimistas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comunidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    OrdemExibicao = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comunidades", x => x.Id);
                    table.CheckConstraint("CK_Comunidades_OrdemExibicao", "[OrdemExibicao] >= 0");
                    table.CheckConstraint("CK_Comunidades_Tipo", "[Tipo] IN (1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "Dizimistas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, collation: "Latin1_General_100_CI_AI"),
                    Cpf = table.Column<string>(type: "char(11)", unicode: false, fixedLength: true, maxLength: 11, nullable: true),
                    Telefone = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: true),
                    ComunidadeId = table.Column<int>(type: "int", nullable: false),
                    DataEntrada = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dizimistas", x => x.Id);
                    table.CheckConstraint("CK_Dizimistas_Status", "[Status] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_Dizimistas_Comunidades_ComunidadeId",
                        column: x => x.ComunidadeId,
                        principalTable: "Comunidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Comunidades",
                columns: new[] { "Id", "Ativa", "AtualizadoEm", "CriadoEm", "Nome", "OrdemExibicao", "Tipo" },
                values: new object[,]
                {
                    { 1, true, null, new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Paróquia São Dimas", 1, 1 },
                    { 2, true, null, new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Santa Teresinha", 2, 2 },
                    { 3, true, null, new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Santo Inácio", 3, 2 },
                    { 4, true, null, new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Santo Expedito", 4, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comunidades_Nome",
                table: "Comunidades",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comunidades_OrdemExibicao",
                table: "Comunidades",
                column: "OrdemExibicao");

            migrationBuilder.CreateIndex(
                name: "UX_Comunidades_MatrizUnica",
                table: "Comunidades",
                column: "Tipo",
                unique: true,
                filter: "[Tipo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Dizimistas_ComunidadeId_Nome",
                table: "Dizimistas",
                columns: new[] { "ComunidadeId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_Dizimistas_Cpf",
                table: "Dizimistas",
                column: "Cpf",
                unique: true,
                filter: "[Cpf] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Dizimistas_Nome",
                table: "Dizimistas",
                column: "Nome");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dizimistas");

            migrationBuilder.DropTable(
                name: "Comunidades");
        }
    }
}
