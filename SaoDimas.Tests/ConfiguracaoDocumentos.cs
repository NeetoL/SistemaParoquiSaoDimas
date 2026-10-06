using System.Runtime.CompilerServices;
namespace SaoDimas.Tests;
internal static class ConfiguracaoDocumentos
{
    [ModuleInitializer]
    internal static void Inicializar() => QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
}
