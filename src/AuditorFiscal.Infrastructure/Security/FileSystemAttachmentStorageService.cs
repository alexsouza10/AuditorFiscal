using AuditorFiscal.Application.Interfaces.Services;

namespace AuditorFiscal.Infrastructure.Security;

/// <summary>
/// Cada foto/anexo vira um arquivo próprio, criptografado com AES-256-GCM, fora do
/// banco de dados. O caminho salvo é relativo ao diretório base, para que backups
/// continuem válidos mesmo se a pasta de dados do aplicativo for movida.
/// </summary>
public class FileSystemAttachmentStorageService(
    IEncryptionService encryptionService,
    IHashService hashService,
    string diretorioBase) : IAttachmentStorageService
{
    public async Task<ArquivoArmazenadoResultado> SalvarAsync(
        Guid ordemServicoId,
        string nomeArquivo,
        byte[] conteudoOriginal,
        CancellationToken ct = default)
    {
        var pastaOrdemServico = Path.Combine(diretorioBase, ordemServicoId.ToString());
        Directory.CreateDirectory(pastaOrdemServico);

        var nomeArmazenado = $"{Guid.NewGuid():N}.enc";
        var caminhoRelativo = Path.Combine(ordemServicoId.ToString(), nomeArmazenado);
        var caminhoCompleto = Path.Combine(diretorioBase, caminhoRelativo);

        var hash = hashService.ComputeSha256(conteudoOriginal);
        var criptografado = encryptionService.Encrypt(conteudoOriginal);

        await File.WriteAllBytesAsync(caminhoCompleto, criptografado, ct);

        return new ArquivoArmazenadoResultado(caminhoRelativo, conteudoOriginal.LongLength, hash);
    }

    public async Task<byte[]> AbrirDecriptografadoAsync(string caminhoArmazenamento, CancellationToken ct = default)
    {
        var caminhoCompleto = Path.Combine(diretorioBase, caminhoArmazenamento);
        var criptografado = await File.ReadAllBytesAsync(caminhoCompleto, ct);
        return encryptionService.Decrypt(criptografado);
    }

    public void Excluir(string caminhoArmazenamento)
    {
        var caminhoCompleto = Path.Combine(diretorioBase, caminhoArmazenamento);
        if (File.Exists(caminhoCompleto))
            File.Delete(caminhoCompleto);
    }
}
