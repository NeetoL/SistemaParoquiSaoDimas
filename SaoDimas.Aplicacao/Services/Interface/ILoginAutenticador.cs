namespace SaoDimas.Aplicacao.Services.Interface;
public interface ILoginAutenticador
{
    string Usuario { get; }
    bool Validar(string usuario, string senha);
}
