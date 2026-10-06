using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaoDimas.Infraestrutura.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class DadosCadastraisEnvelope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bairro",
                table: "Dizimistas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cep",
                table: "Dizimistas",
                type: "varchar(8)",
                unicode: false,
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataNascimento",
                table: "Dizimistas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Endereco",
                table: "Dizimistas",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bairro",
                table: "Dizimistas");

            migrationBuilder.DropColumn(
                name: "Cep",
                table: "Dizimistas");

            migrationBuilder.DropColumn(
                name: "DataNascimento",
                table: "Dizimistas");

            migrationBuilder.DropColumn(
                name: "Endereco",
                table: "Dizimistas");
        }
    }
}
