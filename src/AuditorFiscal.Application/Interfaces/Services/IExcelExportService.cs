using AuditorFiscal.Domain.Entities;

namespace AuditorFiscal.Application.Interfaces.Services;

public interface IExcelExportService
{
    Task ExportarAsync(
        string titulo,
        IReadOnlyList<OrdemServico> ordensServico,
        string caminhoDestino,
        CancellationToken ct = default);
}
