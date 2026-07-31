using AuditorFiscal.Domain.Entities;

namespace AuditorFiscal.Application.Interfaces.Services;

public interface IPrintService
{
    Task ImprimirAsync(OrdemServico ordemServico, CancellationToken ct = default);
    Task ImprimirRelatorioAsync(string titulo, IReadOnlyList<OrdemServico> ordensServico, CancellationToken ct = default);
}
