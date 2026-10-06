namespace SaoDimas.Aplicacao.Services.Interface;
public interface ICredenciaisService { string GerarHash(string senha); bool Verificar(string hash,string senha); }
