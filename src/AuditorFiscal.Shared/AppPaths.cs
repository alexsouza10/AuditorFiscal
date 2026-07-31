namespace AuditorFiscal.Shared;

/// <summary>
/// Único ponto de verdade para os diretórios de dados do aplicativo, usados tanto pela
/// Persistence (arquivo do banco) quanto pela Infrastructure (chave, anexos, backups,
/// logs) — dois projetos irmãos que não podem depender um do outro.
/// </summary>
public static class AppPaths
{
    public static string Raiz { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AuditorFiscal");

    public static string Seguro => Path.Combine(Raiz, "secure");
    public static string Anexos => Path.Combine(Raiz, "attachments");

    /// <summary>
    /// Pasta onde os backups (.afbkp) ficam guardados — nomeada "db" de propósito para que
    /// o usuário a encontre facilmente pelo Explorer e restaure a partir dela quando quiser.
    /// </summary>
    public static string Backups => Path.Combine(Raiz, "db");
    public static string Logs => Path.Combine(Raiz, "logs");
    public static string Config => Path.Combine(Raiz, "config");
    public static string BancoDados => Path.Combine(Raiz, "auditorfiscal.db");
}
