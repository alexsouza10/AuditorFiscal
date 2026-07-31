using AuditorFiscal.Shared;

namespace AuditorFiscal.Domain.Entities;

public class BackupRegistro : EntidadeBase
{
    public string CaminhoArquivo { get; private set; } = string.Empty;
    public long TamanhoBytes { get; private set; }
    public string HashSha256 { get; private set; } = string.Empty;
    public bool Automatico { get; private set; }

    private BackupRegistro()
    {
    }

    public BackupRegistro(string caminhoArquivo, long tamanhoBytes, string hashSha256, bool automatico, DateTimeOffset momento)
    {
        CaminhoArquivo = Guard.NotNullOrWhiteSpace(caminhoArquivo, nameof(caminhoArquivo));
        TamanhoBytes = tamanhoBytes;
        HashSha256 = Guard.NotNullOrWhiteSpace(hashSha256, nameof(hashSha256));
        Automatico = automatico;
        CriadoEm = momento;
        AtualizadoEm = momento;
    }
}
