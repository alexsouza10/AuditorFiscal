namespace AuditorFiscal.Application.Interfaces.Services;

public enum TemaAplicacao
{
    Sistema = 0,
    Claro = 1,
    Escuro = 2
}

public sealed record Preferencias
{
    public TemaAplicacao Tema { get; init; } = TemaAplicacao.Sistema;
    public bool IniciarComWindows { get; init; }
    public bool BackupAutomatico { get; init; } = true;
    public int BackupIntervaloHoras { get; init; } = 24;
    public DateTimeOffset? UltimoBackup { get; init; }
}

/// <summary>
/// Preferências não sensíveis (tema, agendamento de backup). Ficam em JSON simples fora
/// do banco: precisam ser lidas antes de a chave mestra e o SQLCipher serem inicializados.
/// Nenhum dado de auditoria pode ser gravado aqui.
/// </summary>
public interface IPreferencesService
{
    Preferencias Atual { get; }
    void Salvar(Preferencias preferencias);
}
