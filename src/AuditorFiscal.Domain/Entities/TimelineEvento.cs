using AuditorFiscal.Shared;

namespace AuditorFiscal.Domain.Entities;

public class TimelineEvento : EntidadeBase
{
    public Guid OrdemServicoId { get; private set; }
    public DateTimeOffset OcorridoEm { get; private set; }
    public string Descricao { get; private set; } = string.Empty;

    private TimelineEvento()
    {
    }

    public TimelineEvento(Guid ordemServicoId, string descricao, DateTimeOffset ocorridoEm)
    {
        OrdemServicoId = ordemServicoId;
        Descricao = Guard.NotNullOrWhiteSpace(descricao, nameof(descricao));
        OcorridoEm = ocorridoEm;
    }
}
