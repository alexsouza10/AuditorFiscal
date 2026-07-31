namespace AuditorFiscal.Domain.Entities;

public class Anexo : ArquivoArmazenado
{
    private Anexo()
    {
    }

    public Anexo(
        Guid ordemServicoId,
        string nomeOriginal,
        string contentType,
        string caminhoArmazenamento,
        long tamanhoBytes,
        string hashSha256)
        : base(ordemServicoId, nomeOriginal, contentType, caminhoArmazenamento, tamanhoBytes, hashSha256)
    {
    }
}
