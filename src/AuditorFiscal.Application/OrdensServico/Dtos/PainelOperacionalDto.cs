namespace AuditorFiscal.Application.OrdensServico.Dtos;

public sealed record AlertaOrdemServicoDto(
    Guid Id,
    string Numero,
    string Empresa,
    string Descricao,
    int Dias);

public sealed record PainelOperacionalDto(
    IReadOnlyList<AlertaOrdemServicoDto> Atrasadas,
    IReadOnlyList<AlertaOrdemServicoDto> VencemHoje,
    IReadOnlyList<AlertaOrdemServicoDto> VencemEmBreve,
    IReadOnlyList<AlertaOrdemServicoDto> SemMovimentacao,
    int TotalEmAndamento,
    int TotalConcluidas)
{
    public bool TemAlertas => Atrasadas.Count > 0 || VencemHoje.Count > 0 || SemMovimentacao.Count > 0;
}
