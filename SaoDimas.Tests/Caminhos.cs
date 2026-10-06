namespace SaoDimas.Tests;

internal static class Caminhos
{
    /// <summary>
    /// Pasta do SaoDimas.MVC (onde o build gera a logo de impressão usada pelos PDFs).
    /// </summary>
    public static string RaizMvc()
    {
        for (var diretorio = new DirectoryInfo(AppContext.BaseDirectory); diretorio is not null; diretorio = diretorio.Parent)
        {
            var candidato = Path.Combine(diretorio.FullName, "SaoDimas.MVC");
            if (File.Exists(Path.Combine(candidato, "wwwroot", "img", "brasao-impressao.png")))
            {
                return candidato;
            }
        }

        throw new InvalidOperationException("Logo de impressão não encontrada. Compile o SaoDimas.MVC (dotnet build).");
    }
}
