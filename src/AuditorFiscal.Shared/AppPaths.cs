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
    public static string Backups => Path.Combine(Raiz, "backups");
    public static string Logs => Path.Combine(Raiz, "logs");
    public static string Config => Path.Combine(Raiz, "config");
    public static string BancoDados => Path.Combine(Raiz, "auditorfiscal.db");
}
