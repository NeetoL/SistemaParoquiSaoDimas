using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SaoDimas.Aplicacao.Services.Interface;
namespace SaoDimas.Infraestrutura.Services.Implementation;
internal sealed class DocumentoParoquialService:IDocumentoParoquialService
{
 public byte[] Gerar(string titulo,IReadOnlyList<Dictionary<string,string>> registros,string operador)=>Document.Create(document=>{
  document.Page(page=>{page.Size(PageSizes.A4);page.Margin(35);page.DefaultTextStyle(s=>s.FontSize(10));
   page.Header().PaddingBottom(18).Column(c=>{c.Item().Text("PARÓQUIA SÃO DIMAS").FontColor("#623695").Bold().FontSize(14);c.Item().Text(titulo).FontSize(18);});
   page.Content().Column(c=>{if(registros.Count==0)c.Item().Text("Nenhum registro para o período selecionado.");foreach(var registro in registros){c.Item().PaddingBottom(16).BorderBottom(1).BorderColor("#dddddd").PaddingBottom(10).Column(r=>{foreach(var campo in registro){r.Item().Text(t=>{t.Span(campo.Key+": ").SemiBold();t.Span(campo.Value);});}});}if(registros.Count==1)c.Item().PaddingTop(30).Text("Responsável: __________________________________________");});
   page.Footer().AlignCenter().Text(t=>{t.Span("Emitido por "+operador+" · ");t.CurrentPageNumber();t.Span(" / ");t.TotalPages();});
  });
 }).GeneratePdf();
}
