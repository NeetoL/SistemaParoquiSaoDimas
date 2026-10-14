using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace SaoDimas.Infraestrutura.Persistencia.Migrations;
public partial class AniversarioSemAno : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("AniversarioSemAno", "Dizimistas", type: "nvarchar(5)", maxLength: 5, nullable: true);
        migrationBuilder.Sql("UPDATE [Dizimistas] SET [AniversarioSemAno] = REPLACE(LEFT(CONVERT(varchar(10), [DataNascimento], 105), 5), '-', '/') WHERE [DataNascimento] IS NOT NULL;");
        migrationBuilder.DropColumn("DataNascimento", "Dizimistas");
        migrationBuilder.RenameColumn("AniversarioSemAno", "Dizimistas", "DataNascimento");
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new System.NotSupportedException("O ano foi removido. Restaure um backup anterior para recuperar datas de nascimento completas.");
}
