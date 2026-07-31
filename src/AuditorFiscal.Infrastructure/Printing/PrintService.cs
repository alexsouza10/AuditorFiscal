using System.Drawing;
using System.Drawing.Printing;
using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Application.Relatorios;
using AuditorFiscal.Domain.Entities;

namespace AuditorFiscal.Infrastructure.Printing;

/// <summary>
/// Envia o relatório direto para a impressora padrão desenhando na superfície GDI+.
/// Não gera PDF intermediário nem qualquer arquivo temporário em disco, atendendo ao
/// requisito de "nenhum arquivo temporário".
/// </summary>
public class PrintService : IPrintService
{
    private const float MargemEsquerda = 60;
    private const float MargemTopo = 60;
    private const float MargemInferior = 60;
    private const float LarguraRotulo = 150;

    public Task ImprimirAsync(OrdemServico ordemServico, CancellationToken ct = default)
    {
        Imprimir(RelatorioBuilder.DeOrdemServico(ordemServico));
        return Task.CompletedTask;
    }

    public Task ImprimirRelatorioAsync(string titulo, IReadOnlyList<OrdemServico> ordensServico, CancellationToken ct = default)
    {
        Imprimir(RelatorioBuilder.DeLista(titulo, ordensServico));
        return Task.CompletedTask;
    }

    private static void Imprimir(DocumentoRelatorio documento)
    {
        var indiceLinha = 0;

        using var fonteTitulo = new Font("Arial", 16, FontStyle.Bold);
        using var fonteSubtitulo = new Font("Arial", 11, FontStyle.Bold);
        using var fonteRotulo = new Font("Arial", 9, FontStyle.Bold);
        using var fonteTexto = new Font("Arial", 9, FontStyle.Regular);
        using var caneta = new Pen(Color.LightGray);

        using var documentoImpressao = new PrintDocument { DocumentName = documento.Titulo };

        documentoImpressao.PrintPage += (_, argumentos) =>
        {
            var grafico = argumentos.Graphics!;
            var limite = argumentos.PageBounds.Height - MargemInferior;
            var larguraUtil = argumentos.PageBounds.Width - MargemEsquerda * 2;
            var y = MargemTopo;

            while (indiceLinha < documento.Linhas.Count && y < limite)
            {
                var linha = documento.Linhas[indiceLinha];

                switch (linha.Estilo)
                {
                    case EstiloLinha.Titulo:
                        grafico.DrawString(linha.Texto, fonteTitulo, Brushes.Black, MargemEsquerda, y);
                        y += 30;
                        break;

                    case EstiloLinha.Subtitulo:
                        y += 8;
                        grafico.DrawString(linha.Texto, fonteSubtitulo, Brushes.Black, MargemEsquerda, y);
                        y += 22;
                        break;

                    case EstiloLinha.Campo:
                        grafico.DrawString(linha.Texto, fonteRotulo, Brushes.Black, MargemEsquerda, y);
                        y += DesenharTexto(grafico, linha.Valor ?? string.Empty, fonteTexto,
                            MargemEsquerda + LarguraRotulo, y, larguraUtil - LarguraRotulo);
                        break;

                    case EstiloLinha.Texto:
                        y += DesenharTexto(grafico, linha.Texto, fonteTexto, MargemEsquerda, y, larguraUtil);
                        break;

                    case EstiloLinha.Separador:
                        y += 6;
                        grafico.DrawLine(caneta, MargemEsquerda, y, MargemEsquerda + larguraUtil, y);
                        y += 12;
                        break;
                }

                indiceLinha++;
            }

            argumentos.HasMorePages = indiceLinha < documento.Linhas.Count;
        };

        documentoImpressao.Print();
    }

    private static float DesenharTexto(Graphics grafico, string texto, Font fonte, float x, float y, float larguraMaxima)
    {
        var area = new RectangleF(x, y, larguraMaxima, float.MaxValue);
        var tamanho = grafico.MeasureString(texto, fonte, new SizeF(larguraMaxima, float.MaxValue));
        grafico.DrawString(texto, fonte, Brushes.Black, area);
        return Math.Max(tamanho.Height, fonte.GetHeight(grafico)) + 2;
    }
}
