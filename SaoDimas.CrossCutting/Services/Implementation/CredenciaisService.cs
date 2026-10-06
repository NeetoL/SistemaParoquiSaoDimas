using Microsoft.AspNetCore.Identity;
using SaoDimas.Aplicacao.Services.Interface;
namespace SaoDimas.CrossCutting.Services.Implementation;
internal sealed class CredenciaisService : ICredenciaisService
{
 private readonly PasswordHasher<string> _hasher=new();
 public string GerarHash(string senha)=>_hasher.HashPassword("SaoDimas",senha);
 public bool Verificar(string hash,string senha) { try { return _hasher.VerifyHashedPassword("SaoDimas",hash,senha)!=PasswordVerificationResult.Failed; } catch(FormatException) { return false; } }
}
