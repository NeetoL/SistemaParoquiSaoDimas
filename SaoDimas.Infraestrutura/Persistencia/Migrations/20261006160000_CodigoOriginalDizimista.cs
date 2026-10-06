using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace SaoDimas.Infraestrutura.Persistencia.Migrations;
public partial class CodigoOriginalDizimista : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(name: "CodigoOriginal", table: "Dizimistas", type: "nvarchar(50)", maxLength: 50, nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "CodigoOriginal", table: "Dizimistas");
}