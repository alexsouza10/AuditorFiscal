namespace AuditorFiscal.Application.Interfaces.Services;

/// <summary>
/// Fornece a chave mestra de 256 bits usada para criptografar arquivos e o banco de dados.
/// A chave é protegida em repouso via DPAPI e existe em texto puro apenas em memória.
/// </summary>
public interface IMasterKeyProvider
{
    byte[] ObterChave();
}
