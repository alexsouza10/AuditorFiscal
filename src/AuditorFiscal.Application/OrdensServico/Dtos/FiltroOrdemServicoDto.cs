using AuditorFiscal.Domain.Enums;

namespace AuditorFiscal.Application.OrdensServico.Dtos;

public sealed record FiltroOrdemServicoDto
{
    public string? Termo { get; init; }
    public SituacaoOS? Situacao { get; init; }
    public Guid? TagId { get; init; }
    public bool SomenteFavoritos { get; init; }
    public DateOnly? DataInicio { get; init; }
    public DateOnly? DataFim { get; init; }
}
