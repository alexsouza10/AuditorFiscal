using AuditorFiscal.Domain.Entities;

namespace AuditorFiscal.Application.Interfaces.Services;

/// <summary>
/// Gera arquivos PDF comuns (sem criptografia nem senha), prontos para compartilhar ou
/// imprimir fora do aplicativo. Não confundir com IAttachmentStorageService, usado para
/// anexos guardados internamente de forma criptografada.
/// </summary>
public interface IPdfExportService
{
    Task ExportarOrdemServicoAsync(OrdemServico ordemServico, string caminhoDestino, CancellationToken ct = default);

    Task ExportarRelatorioAsync(
        string titulo,
        IReadOnlyList<OrdemServico> ordensServico,
        string caminhoDestino,
        CancellationToken ct = default);
}
