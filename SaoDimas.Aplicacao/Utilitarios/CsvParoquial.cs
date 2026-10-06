using System.Text;
namespace SaoDimas.Aplicacao.Utilitarios;
public static class CsvParoquial{
 public static List<string[]> Ler(string csv){var linhas=new List<string[]>();var linha=new List<string>();var campo=new StringBuilder();bool aspas=false;for(int i=0;i<csv.Length;i++){var c=csv[i];if(c=='"'){if(aspas&&i+1<csv.Length&&csv[i+1]=='"'){campo.Append('"');i++;}else aspas=!aspas;}else if(c==';'&&!aspas){linha.Add(campo.ToString());campo.Clear();}else if(c=='\n'&&!aspas){linha.Add(campo.ToString().TrimEnd('\r'));campo.Clear();if(linha.Any(s=>s.Length>0))linhas.Add(linha.ToArray());linha.Clear();}else campo.Append(c);}if(aspas)throw new FormatException();if(campo.Length>0||linha.Count>0){linha.Add(campo.ToString());linhas.Add(linha.ToArray());}return linhas;}
}
