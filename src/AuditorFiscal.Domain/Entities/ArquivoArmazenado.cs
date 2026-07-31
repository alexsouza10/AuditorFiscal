using AuditorFiscal.Shared;

namespace AuditorFiscal.Domain.Entities;

/// <summary>
/// Metadados de um arquivo cujo conteúdo real fica em um arquivo separado no disco,
/// criptografado com AES-256-GCM (ver AuditorFiscal.Infrastructure.Security). Nenhum
/// byte de conteúdo é mantido aqui nem no banco de dados.
/// </summary>
public abstract class ArquivoArmazenado : EntidadeBase
{
    public Guid OrdemServicoId { get; private set; }
    public string NomeOriginal { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public string CaminhoArmazenamento { get; private set; } = string.Empty;
    public long TamanhoBytes { get; private set; }
    public string HashSha256 { get; private set; } = string.Empty;

    protected ArquivoArmazenado()
    {
    }

    protected ArquivoArmazenado(
        Guid ordemServicoId,
        string nomeOriginal,
        string contentType,
        string caminhoArmazenamento,
        long tamanhoBytes,
        string hashSha256)
    {
        OrdemServicoId = ordemServicoId;
        NomeOriginal = Guard.NotNullOrWhiteSpace(nomeOriginal, nameof(nomeOriginal));
        ContentType = Guard.NotNullOrWhiteSpace(contentType, nameof(contentType));
        CaminhoArmazenamento = Guard.NotNullOrWhiteSpace(caminhoArmazenamento, nameof(caminhoArmazenamento));
        TamanhoBytes = tamanhoBytes;
        HashSha256 = Guard.NotNullOrWhiteSpace(hashSha256, nameof(hashSha256));
    }
}
