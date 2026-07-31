using System.Security.Cryptography;
using AuditorFiscal.Application.Interfaces.Services;

namespace AuditorFiscal.Infrastructure.Security;

/// <summary>
/// Gera (na primeira execução) e protege em repouso, via DPAPI, a chave mestra de 256
/// bits usada para criptografar arquivos e abrir o SQLite (SQLCipher). A chave só existe
/// em texto puro em memória, nunca em disco.
/// </summary>
public class DpapiMasterKeyProvider : IMasterKeyProvider
{
    private const string NomeArquivoChave = "master.key";
    private static readonly byte[] Entropia = "AuditorFiscal.MasterKey.v1"u8.ToArray();

    private readonly string _caminhoArquivoChave;
    private readonly Lazy<byte[]> _chave;

    public DpapiMasterKeyProvider(string diretorioSeguro)
    {
        Directory.CreateDirectory(diretorioSeguro);
        _caminhoArquivoChave = Path.Combine(diretorioSeguro, NomeArquivoChave);
        _chave = new Lazy<byte[]>(CarregarOuCriarChave);
    }

    public byte[] ObterChave() => _chave.Value;

    private byte[] CarregarOuCriarChave()
    {
        if (File.Exists(_caminhoArquivoChave))
        {
            var chaveProtegida = File.ReadAllBytes(_caminhoArquivoChave);
            return ProtectedData.Unprotect(chaveProtegida, Entropia, DataProtectionScope.CurrentUser);
        }

        var chave = RandomNumberGenerator.GetBytes(32);
        var chaveRecemProtegida = ProtectedData.Protect(chave, Entropia, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_caminhoArquivoChave, chaveRecemProtegida);
        return chave;
    }
}
