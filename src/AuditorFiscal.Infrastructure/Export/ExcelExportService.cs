using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;
using ClosedXML.Excel;

namespace AuditorFiscal.Infrastructure.Export;

public class ExcelExportService : IExcelExportService
{
    public Task ExportarAsync(string titulo, IReadOnlyList<OrdemServico> ordensServico, string caminhoDestino, CancellationToken ct = default)
    {
        using var planilha = new XLWorkbook();
        var aba = planilha.AddWorksheet("Ordens de Serviço");

        string[] cabecalhos =
        [
            "Número", "Empresa", "CNPJ", "Endereço", "Cidade", "Responsável",
            "Data", "Hora", "Situação", "Tipo de Auditoria", "Favorito", "Tags",
            "Fotos", "Anexos", "Latitude", "Longitude", "Observações"
        ];

        for (var coluna = 0; coluna < cabecalhos.Length; coluna++)
        {
            var celula = aba.Cell(1, coluna + 1);
            celula.Value = cabecalhos[coluna];
            celula.Style.Font.Bold = true;
            celula.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var linha = 2;
        foreach (var os in ordensServico.OrderBy(o => o.Data).ThenBy(o => o.Hora))
        {
            aba.Cell(linha, 1).Value = os.Numero;
            aba.Cell(linha, 2).Value = os.Empresa;
            aba.Cell(linha, 3).Value = os.Cnpj.Formatado();
            aba.Cell(linha, 4).Value = os.Endereco;
            aba.Cell(linha, 5).Value = os.Cidade;
            aba.Cell(linha, 6).Value = os.Responsavel;
            aba.Cell(linha, 7).Value = os.Data.ToDateTime(TimeOnly.MinValue);
            aba.Cell(linha, 7).Style.DateFormat.Format = "dd/MM/yyyy";
            aba.Cell(linha, 8).Value = os.Hora.ToString("HH\\:mm");
            aba.Cell(linha, 9).Value = os.Situacao.Descricao();
            aba.Cell(linha, 10).Value = os.TipoAuditoria?.Nome ?? string.Empty;
            aba.Cell(linha, 11).Value = os.Favorito ? "Sim" : "Não";
            aba.Cell(linha, 12).Value = string.Join(", ", os.Tags.Select(t => t.Nome));
            aba.Cell(linha, 13).Value = os.Fotos.Count;
            aba.Cell(linha, 14).Value = os.Anexos.Count;
            aba.Cell(linha, 15).Value = os.Coordenada?.Latitude;
            aba.Cell(linha, 16).Value = os.Coordenada?.Longitude;
            aba.Cell(linha, 17).Value = os.Observacoes ?? string.Empty;
            linha++;
        }

        aba.Range(1, 1, Math.Max(1, linha - 1), cabecalhos.Length).SetAutoFilter();
        aba.SheetView.FreezeRows(1);
        aba.Columns().AdjustToContents(10d, 60d);

        planilha.SaveAs(caminhoDestino);
        return Task.CompletedTask;
    }
}
