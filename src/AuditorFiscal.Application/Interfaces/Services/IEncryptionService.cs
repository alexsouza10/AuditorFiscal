namespace AuditorFiscal.Application.Interfaces.Services;

/// <summary>
/// Criptografia de arquivos (fotos, anexos, backups) com AES-256-GCM.
/// Não deve ser usado para o banco de dados: a criptografia do SQLite é feita
/// em nível de página pelo SQLCipher (ver AuditorFiscal.Persistence).
/// </summary>
public interface IEncryptionService
{
    byte[] Encrypt(byte[] dadosOriginais);
    byte[] Decrypt(byte[] dadosCriptografados);
}
