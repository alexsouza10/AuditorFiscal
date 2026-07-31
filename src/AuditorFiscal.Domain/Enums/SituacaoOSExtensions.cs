namespace AuditorFiscal.Domain.Enums;

public static class SituacaoOSExtensions
{
    public static string Descricao(this SituacaoOS situacao) => situacao switch
    {
        SituacaoOS.Agendada => "Agendada",
        SituacaoOS.EmAndamento => "Em andamento",
        SituacaoOS.Concluida => "Concluída",
        SituacaoOS.Adiada => "Adiada",
        SituacaoOS.Cancelada => "Cancelada",
        _ => situacao.ToString()
    };

    /// <summary>
    /// Cor usada de forma consistente em toda a aplicação (agenda, dashboard, relatórios)
    /// para que a mesma situação seja sempre reconhecida pela mesma cor.
    /// </summary>
    public static string Cor(this SituacaoOS situacao) => situacao switch
    {
        SituacaoOS.Agendada => "#3B82F6",
        SituacaoOS.EmAndamento => "#F59E0B",
        SituacaoOS.Concluida => "#22C55E",
        SituacaoOS.Adiada => "#94A3B8",
        SituacaoOS.Cancelada => "#EF4444",
        _ => "#94A3B8"
    };
}
