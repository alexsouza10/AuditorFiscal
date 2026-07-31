using AuditorFiscal.Domain.Entities;

namespace AuditorFiscal.Application.Interfaces.Services;

public interface IBackupService
{
    /// <summary>Cria um backup criptografado (AES-256-GCM) do banco e dos anexos.</summary>
    Task<BackupRegistro> CriarBackupAsync(bool automatico, string? caminhoDestino = null, CancellationToken ct = default);

    /// <summary>Restaura um backup previamente criado. Exige reinício do aplicativo.</summary>
    Task RestaurarBackupAsync(string caminhoArquivo, CancellationToken ct = default);

    IReadOnlyList<string> ListarBackupsLocais();
}
