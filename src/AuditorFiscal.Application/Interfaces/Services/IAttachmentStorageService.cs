namespace AuditorFiscal.Application.Interfaces.Services;

public sealed record ArquivoArmazenadoResultado(string CaminhoArmazenamento, long TamanhoBytes, string HashSha256);

/// <summary>
/// Persiste fotos e anexos como arquivos individuais criptografados (AES-256-GCM) em disco.
/// O conteúdo nunca é gravado em texto puro nem em arquivos temporários.
/// </summary>
public interface IAttachmentStorageService
{
    Task<ArquivoArmazenadoResultado> SalvarAsync(
        Guid ordemServicoId,
        string nomeArquivo,
        byte[] conteudoOriginal,
        CancellationToken ct = default);

    Task<byte[]> AbrirDecriptografadoAsync(string caminhoArmazenamento, CancellationToken ct = default);

    void Excluir(string caminhoArmazenamento);
}
