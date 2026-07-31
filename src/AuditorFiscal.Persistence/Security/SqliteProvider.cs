namespace AuditorFiscal.Persistence.Security;

/// <summary>
/// Registra o provedor SQLitePCLRaw habilitado para SQLCipher exatamente uma vez por
/// processo. Precisa acontecer antes de qualquer SqliteConnection ser aberta, senão o
/// SQLite abre o arquivo sem criptografia.
/// </summary>
public static class SqliteProvider
{
    private static readonly Lazy<bool> Inicializacao = new(() =>
    {
        SQLitePCL.Batteries_V2.Init();
        return true;
    });

    public static void GarantirInicializado() => _ = Inicializacao.Value;
}
