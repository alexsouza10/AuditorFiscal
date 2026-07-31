using AuditorFiscal.Domain.Entities;

namespace AuditorFiscal.Application.Interfaces.Services;

public interface IBackupService
{
    /// <summary>Cria um backup criptografado (AES-256-GCM) do banco e dos anexos.</summary>
    Task<BackupRegistro> CriarBackupAsync(bool automatico, string? caminhoDestino = null, CancellationToken ct = default);

    /// <summary>
    /// Valida e agenda a restauração de um backup. Não sobrescreve nada agora — o banco
    /// atual está aberto pela conexão do EF Core enquanto o app roda, e tentar sobrescrevê-lo
    /// nesse momento falha. A restauração de fato só acontece no próximo início do
    /// aplicativo, via <see cref="AplicarRestauracaoPendenteAsync"/>, antes de qualquer
    /// conexão com o banco ser aberta.
    /// </summary>
    Task RestaurarBackupAsync(string caminhoArquivo, CancellationToken ct = default);

    /// <summary>
    /// Aplica uma restauração agendada por <see cref="RestaurarBackupAsync"/>, se houver
    /// uma pendente. Deve ser chamado no início do app, antes de qualquer acesso ao banco.
    /// </summary>
    Task AplicarRestauracaoPendenteAsync(CancellationToken ct = default);

    IReadOnlyList<string> ListarBackupsLocais();
}
