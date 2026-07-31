using Microsoft.Data.Sqlite;

namespace AuditorFiscal.Persistence.Security;

public static class SqliteConnectionStringFactory
{
    /// <summary>
    /// Monta a connection string do SQLCipher usando a chave de 256 bits em formato
    /// hexadecimal bruto (x'...'), evitando o PBKDF2 que o SQLCipher aplicaria a uma
    /// senha textual — a chave já tem entropia total, então derivar novamente seria
    /// custo de CPU sem ganho de segurança.
    /// </summary>
    public static string Criar(string caminhoArquivo, byte[] chave)
    {
        SqliteProvider.GarantirInicializado();

        var chaveHex = Convert.ToHexString(chave);

        return new SqliteConnectionStringBuilder
        {
            DataSource = caminhoArquivo,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Password = $"x'{chaveHex}'"
        }.ToString();
    }
}
