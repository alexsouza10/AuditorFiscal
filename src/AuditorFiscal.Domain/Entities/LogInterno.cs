using AuditorFiscal.Shared;

namespace AuditorFiscal.Domain.Entities;

/// <summary>
/// Trilha de auditoria em nível de aplicação, visível na UI (tela Banco de Dados).
/// Distinta dos logs técnicos do Serilog: aqui só entram ações do usuário
/// (criar/editar/excluir OS, backup, restauração, exportação), nunca dados sensíveis.
/// </summary>
public class LogInterno : EntidadeBase
{
    public DateTimeOffset OcorridoEm { get; private set; }
    public string Acao { get; private set; } = string.Empty;
    public string? Detalhes { get; private set; }

    private LogInterno()
    {
    }

    public LogInterno(string acao, DateTimeOffset ocorridoEm, string? detalhes = null)
    {
        Acao = Guard.NotNullOrWhiteSpace(acao, nameof(acao));
        OcorridoEm = ocorridoEm;
        Detalhes = detalhes;
    }
}
