using System.Text;
using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;
using AuditorFiscal.Domain.ValueObjects;
using AuditorFiscal.Infrastructure.Export;
using AwesomeAssertions;

namespace IntegrationTests.Export;

public class ExportacaoTests : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), $"af_export_{Guid.NewGuid():N}");

    public ExportacaoTests() => Directory.CreateDirectory(_pasta);

    private static OrdemServico CriarOrdemServico(string numero = "OS-2026-0001") => new(
        numero, "Empresa Exportação Ltda", Cnpj.Criar("11.444.777/0001-61"),
        "Rua Exemplo, 123", "São Paulo", "João da Silva", TipoFiscalizacao.Mista,
        new DateOnly(2026, 8, 12), new DateOnly(2026, 8, 14), new DateOnly(2026, 8, 20), new DateOnly(2026, 8, 28),
        new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 20), new DateOnly(2026, 9, 30),
        DateTimeOffset.UtcNow,
        "Observações da auditoria com texto longo o suficiente para forçar a quebra de linha no relatório gerado.",
        new Coordenada(-23.55, -46.63));

    [Fact]
    public async Task ExportarPdfDeOrdemServico_DeveGerarArquivoPdfLegivel()
    {
        var destino = Path.Combine(_pasta, "os.pdf");

        await new PdfExportService().ExportarOrdemServicoAsync(CriarOrdemServico(), destino);

        File.Exists(destino).Should().BeTrue();

        var bytes = await File.ReadAllBytesAsync(destino);
        bytes.Length.Should().BeGreaterThan(500);
        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-",
            "o arquivo exportado precisa ser um PDF comum, abrível fora do aplicativo");
    }

    [Fact]
    public async Task ExportarPdfDeRelatorio_DeveGerarArquivoComVariasOrdens()
    {
        var destino = Path.Combine(_pasta, "relatorio.pdf");
        var ordens = Enumerable.Range(1, 40).Select(i => CriarOrdemServico($"OS-2026-{i:D4}")).ToList();

        await new PdfExportService().ExportarRelatorioAsync("Relatório de teste", ordens, destino);

        File.Exists(destino).Should().BeTrue();
        (await File.ReadAllBytesAsync(destino)).Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task ExportarExcel_DeveGerarPlanilhaValida()
    {
        var destino = Path.Combine(_pasta, "ordens.xlsx");

        await new ExcelExportService().ExportarAsync("Ordens", [CriarOrdemServico()], destino);

        File.Exists(destino).Should().BeTrue();

        var bytes = await File.ReadAllBytesAsync(destino);
        // XLSX é um zip: assinatura "PK".
        bytes[0].Should().Be((byte)'P');
        bytes[1].Should().Be((byte)'K');
    }

    [Fact]
    public async Task ExportarExcel_ComListaVazia_NaoDeveFalhar()
    {
        var destino = Path.Combine(_pasta, "vazio.xlsx");

        await new ExcelExportService().ExportarAsync("Vazio", [], destino);

        File.Exists(destino).Should().BeTrue();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_pasta))
                Directory.Delete(_pasta, recursive: true);
        }
        catch (IOException)
        {
            // Limpeza best-effort.
        }
    }
}
