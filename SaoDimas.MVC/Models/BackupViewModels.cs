using SaoDimas.Aplicacao.Dtos;
namespace SaoDimas.MVC.Models;
public sealed record BackupsViewModel(IReadOnlyList<BackupParoquial> Backups);
public sealed record RestaurarBackupViewModel(string Nome,string Hash,int Dizimistas,int Registros,int Eventos);
