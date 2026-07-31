using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Application.Relatorios;
using AuditorFiscal.Domain.Entities;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace AuditorFiscal.Infrastructure.Export;

/// <summary>
/// Gera PDFs comuns (sem senha) a partir do DocumentoRelatorio. O arquivo é escrito
/// diretamente no destino escolhido pelo usuário, sem passar por arquivo temporário.
/// </summary>
public class PdfExportService : IPdfExportService
{
    private const double MargemEsquerda = 50;
    private const double MargemTopo = 50;
    private const double MargemInferior = 50;
    private const double LarguraRotulo = 130;

    public Task ExportarOrdemServicoAsync(OrdemServico ordemServico, string caminhoDestino, CancellationToken ct = default)
    {
        Gerar(RelatorioBuilder.DeOrdemServico(ordemServico), caminhoDestino);
        return Task.CompletedTask;
    }

    public Task ExportarRelatorioAsync(string titulo, IReadOnlyList<OrdemServico> ordensServico, string caminhoDestino, CancellationToken ct = default)
    {
        Gerar(RelatorioBuilder.DeLista(titulo, ordensServico), caminhoDestino);
        return Task.CompletedTask;
    }

    private static void Gerar(DocumentoRelatorio documento, string caminhoDestino)
    {
        using var pdf = new PdfDocument();
        pdf.Info.Title = documento.Titulo;

        var pagina = pdf.AddPage();
        var grafico = XGraphics.FromPdfPage(pagina);
        var y = MargemTopo;

        var fonteTitulo = new XFont("Arial", 18, XFontStyleEx.Bold);
        var fonteSubtitulo = new XFont("Arial", 12, XFontStyleEx.Bold);
        var fonteRotulo = new XFont("Arial", 10, XFontStyleEx.Bold);
        var fonteTexto = new XFont("Arial", 10, XFontStyleEx.Regular);
        var larguraUtil = pagina.Width.Point - MargemEsquerda * 2;

        foreach (var linha in documento.Linhas)
        {
            if (y > pagina.Height.Point - MargemInferior)
            {
                pagina = pdf.AddPage();
                grafico.Dispose();
                grafico = XGraphics.FromPdfPage(pagina);
                y = MargemTopo;
            }

            switch (linha.Estilo)
            {
                case EstiloLinha.Titulo:
                    grafico.DrawString(linha.Texto, fonteTitulo, XBrushes.Black, new XPoint(MargemEsquerda, y));
                    y += 26;
                    break;

                case EstiloLinha.Subtitulo:
                    y += 6;
                    grafico.DrawString(linha.Texto, fonteSubtitulo, XBrushes.Black, new XPoint(MargemEsquerda, y));
                    y += 18;
                    break;

                case EstiloLinha.Campo:
                    grafico.DrawString(linha.Texto, fonteRotulo, XBrushes.Black, new XPoint(MargemEsquerda, y));
                    y = DesenharTextoQuebrado(
                        grafico, linha.Valor ?? string.Empty, fonteTexto,
                        MargemEsquerda + LarguraRotulo, y, larguraUtil - LarguraRotulo);
                    break;

                case EstiloLinha.Texto:
                    y = DesenharTextoQuebrado(grafico, linha.Texto, fonteTexto, MargemEsquerda, y, larguraUtil);
                    break;

                case EstiloLinha.Separador:
                    y += 4;
                    grafico.DrawLine(XPens.LightGray, MargemEsquerda, y, MargemEsquerda + larguraUtil, y);
                    y += 10;
                    break;
            }
        }

        grafico.Dispose();
        pdf.Save(caminhoDestino);
    }

    private static double DesenharTextoQuebrado(XGraphics grafico, string texto, XFont fonte, double x, double y, double larguraMaxima)
    {
        const double alturaLinha = 15;

        foreach (var paragrafo in texto.Split('\n'))
        {
            var linhaAtual = string.Empty;

            foreach (var palavra in paragrafo.Split(' '))
            {
                var candidata = string.IsNullOrEmpty(linhaAtual) ? palavra : $"{linhaAtual} {palavra}";

                if (grafico.MeasureString(candidata, fonte).Width > larguraMaxima && linhaAtual.Length > 0)
                {
                    grafico.DrawString(linhaAtual, fonte, XBrushes.Black, new XPoint(x, y));
                    y += alturaLinha;
                    linhaAtual = palavra;
                }
                else
                {
                    linhaAtual = candidata;
                }
            }

            grafico.DrawString(linhaAtual, fonte, XBrushes.Black, new XPoint(x, y));
            y += alturaLinha;
        }

        return y;
    }
}
