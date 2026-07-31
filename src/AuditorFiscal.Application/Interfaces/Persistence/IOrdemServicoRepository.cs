using AuditorFiscal.Application.OrdensServico.Dtos;
using AuditorFiscal.Domain.Entities;

namespace AuditorFiscal.Application.Interfaces.Persistence;

public interface IOrdemServicoRepository : IRepository<OrdemServico>
{
    Task<OrdemServico?> ObterComDetalhesAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<OrdemServico>> BuscarAsync(FiltroOrdemServicoDto filtro, CancellationToken ct = default);
    Task<IReadOnlyList<OrdemServico>> ObterPorPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken ct = default);
    Task<IReadOnlyList<OrdemServico>> ObterPorEmpresaAsync(string empresa, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListarEmpresasAsync(CancellationToken ct = default);
    Task<bool> NumeroJaExisteAsync(string numero, Guid? ignorarId = null, CancellationToken ct = default);
    Task<string> SugerirProximoNumeroAsync(CancellationToken ct = default);
}
