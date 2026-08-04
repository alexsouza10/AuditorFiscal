using AuditorFiscal.Application.Interfaces.Persistence;
using AuditorFiscal.Application.OrdensServico.Dtos;
using AuditorFiscal.Domain.Enums;

namespace AuditorFiscal.Application.OrdensServico;

/// <summary>
/// Painel de Controle Operacional: transforma as datas do fluxo SFIT em avisos acionáveis
/// (atrasadas, prazos próximos, O.S. esquecidas) em vez de deixar o auditor caçar essas
/// informações manualmente entre as O.S. cadastradas.
/// </summary>
public class PainelOperacionalService(IUnitOfWork unitOfWork)
{
    public const int DiasJanelaPrazo = 7;
    public const int DiasSemMovimentacaoLimite = 15;

    public async Task<PainelOperacionalDto> ObterAsync(CancellationToken ct = default)
    {
        var todas = await unitOfWork.OrdensServico.ListarAsync(ct);
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        var atrasadas = todas
            .Where(o => o.EstaAtrasada(hoje))
            .OrderByDescending(o => o.DiasAtraso(hoje))
            .Select(o => new AlertaOrdemServicoDto(o.Id, o.Numero, o.Empresa, "Data final", o.DiasAtraso(hoje)))
            .ToList();

        var vencemHoje = new List<AlertaOrdemServicoDto>();
        var vencemEmBreve = new List<AlertaOrdemServicoDto>();

        foreach (var ordem in todas.Where(o => o.Ativa))
        {
            foreach (var (etapa, data) in ordem.PrazosProximos(hoje, DiasJanelaPrazo))
            {
                var dias = data.DayNumber - hoje.DayNumber;
                var alerta = new AlertaOrdemServicoDto(ordem.Id, ordem.Numero, ordem.Empresa, etapa, dias);
                (dias == 0 ? vencemHoje : vencemEmBreve).Add(alerta);
            }
        }

        var semMovimentacao = todas
            .Where(o => o.Situacao == SituacaoOS.EmAndamento && o.DiasSemMovimentacao(hoje) >= DiasSemMovimentacaoLimite)
            .OrderByDescending(o => o.DiasSemMovimentacao(hoje))
            .Select(o => new AlertaOrdemServicoDto(o.Id, o.Numero, o.Empresa, "Sem movimentação", o.DiasSemMovimentacao(hoje)))
            .ToList();

        return new PainelOperacionalDto(
            atrasadas,
            vencemHoje,
            vencemEmBreve.OrderBy(a => a.Dias).ToList(),
            semMovimentacao,
            todas.Count(o => o.Situacao == SituacaoOS.EmAndamento),
            todas.Count(o => o.Situacao == SituacaoOS.Concluida));
    }
}
