namespace AuditorFiscal.Domain.Entities;

public class Foto : ArquivoArmazenado
{
    public DateTimeOffset DataCaptura { get; private set; }

    private Foto()
    {
    }

    public Foto(
        Guid ordemServicoId,
        string nomeOriginal,
        string contentType,
        string caminhoArmazenamento,
        long tamanhoBytes,
        string hashSha256,
        DateTimeOffset dataCaptura)
        : base(ordemServicoId, nomeOriginal, contentType, caminhoArmazenamento, tamanhoBytes, hashSha256)
    {
        DataCaptura = dataCaptura;
    }
}
