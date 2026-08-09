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
            "Número", "Descrição / Cliente", "CNPJ", "Endereço", "Cidade", "Responsável",
            "Fiscalização", "Auditor", "Recebimento SFIT", "Abertura SFIT", "Data Fiscalização", "Prazo NAD", "Prazo NCO",
            "Elaboração Autos", "Data Final", "Situação", "Favorito",
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
        foreach (var os in ordensServico.OrderBy(o => o.RecebimentoSfit))
        {
            aba.Cell(linha, 1).Value = os.Numero;
            aba.Cell(linha, 2).Value = os.Empresa;
            aba.Cell(linha, 3).Value = os.Cnpj.Formatado();
            aba.Cell(linha, 4).Value = os.Endereco;
            aba.Cell(linha, 5).Value = os.Cidade;
            aba.Cell(linha, 6).Value = os.Responsavel;
            aba.Cell(linha, 7).Value = os.Fiscalizacao.Descricao();
            aba.Cell(linha, 8).Value = os.PapelAuditor.Descricao();

            aba.Cell(linha, 9).Value = os.RecebimentoSfit.ToDateTime(TimeOnly.MinValue);
            aba.Cell(linha, 9).Style.DateFormat.Format = "dd/MM/yyyy";
            aba.Cell(linha, 10).Value = os.AberturaSfit.ToDateTime(TimeOnly.MinValue);
            aba.Cell(linha, 10).Style.DateFormat.Format = "dd/MM/yyyy";
            aba.Cell(linha, 11).Value = os.DataFiscalizacao.ToDateTime(TimeOnly.MinValue);
            aba.Cell(linha, 11).Style.DateFormat.Format = "dd/MM/yyyy";
            aba.Cell(linha, 12).Value = os.PrazoNad.ToDateTime(TimeOnly.MinValue);
            aba.Cell(linha, 12).Style.DateFormat.Format = "dd/MM/yyyy";
            aba.Cell(linha, 13).Value = os.PrazoNco.ToDateTime(TimeOnly.MinValue);
            aba.Cell(linha, 13).Style.DateFormat.Format = "dd/MM/yyyy";
            aba.Cell(linha, 14).Value = os.ElaboracaoAutos.ToDateTime(TimeOnly.MinValue);
            aba.Cell(linha, 14).Style.DateFormat.Format = "dd/MM/yyyy";
            aba.Cell(linha, 15).Value = os.DataFinal.ToDateTime(TimeOnly.MinValue);
            aba.Cell(linha, 15).Style.DateFormat.Format = "dd/MM/yyyy";

            aba.Cell(linha, 16).Value = os.Situacao.Descricao();
            aba.Cell(linha, 17).Value = os.Favorito ? "Sim" : "Não";
            aba.Cell(linha, 18).Value = os.Fotos.Count;
            aba.Cell(linha, 19).Value = os.Anexos.Count;
            aba.Cell(linha, 20).Value = os.Coordenada?.Latitude;
            aba.Cell(linha, 21).Value = os.Coordenada?.Longitude;
            aba.Cell(linha, 22).Value = os.Observacoes ?? string.Empty;
            linha++;
        }

        aba.Range(1, 1, Math.Max(1, linha - 1), cabecalhos.Length).SetAutoFilter();
        aba.SheetView.FreezeRows(1);
        aba.Columns().AdjustToContents(10d, 60d);

        planilha.SaveAs(caminhoDestino);
        return Task.CompletedTask;
    }
}
