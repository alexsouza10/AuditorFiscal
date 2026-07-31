using System.Security.Cryptography;
using System.Text;
using AuditorFiscal.Application.Interfaces.Persistence;
using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Application.OrdensServico;
using AuditorFiscal.Application.OrdensServico.Dtos;
using AuditorFiscal.Application.OrdensServico.Validators;
using AuditorFiscal.Domain.Enums;
using AuditorFiscal.Infrastructure.Security;
using AuditorFiscal.Persistence;
using AuditorFiscal.Persistence.Configurations;
using AuditorFiscal.Persistence.Security;
using AuditorFiscal.Shared;
using AwesomeAssertions;
using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Persistence;

/// <summary>
/// Exercita o caminho completo do caso de uso contra um banco SQLCipher real e o
/// armazenamento criptografado de anexos em disco.
/// </summary>
public class OrdemServicoFluxoTests : IDisposable
{
    private sealed class ChaveFixaProvider(byte[] chave) : IMasterKeyProvider
    {
        public byte[] ObterChave() => chave;
    }

    private readonly string _pastaTemporaria = Path.Combine(Path.GetTempPath(), $"af_fluxo_{Guid.NewGuid():N}");
    private readonly byte[] _chave = RandomNumberGenerator.GetBytes(32);
    private readonly string _caminhoBanco;
    private readonly IAttachmentStorageService _armazenamento;

    public OrdemServicoFluxoTests()
    {
        Directory.CreateDirectory(_pastaTemporaria);
        _caminhoBanco = Path.Combine(_pastaTemporaria, "teste.db");

        var chaveProvider = new ChaveFixaProvider(_chave);
        var hash = new Sha256HashService();
        _armazenamento = new FileSystemAttachmentStorageService(
            new AesGcmEncryptionService(chaveProvider), hash, Path.Combine(_pastaTemporaria, "anexos"));
    }

    [Fact]
    public async Task CriarComArquivos_DevePersistirOsEAnexosCriptografadosDeUmaVez()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();

        var servico = CriarServico(contexto);
        var conteudoFoto = Encoding.UTF8.GetBytes("conteudo-da-foto");

        var resultado = await servico.CriarAsync(
            NovaOrdemDto("OS-2026-9001"),
            [new NovoArquivoDto("foto.jpg", "image/jpeg", conteudoFoto, TipoArquivo.Foto)]);

        resultado.IsSuccess.Should().BeTrue();

        var ordemServico = await servico.ObterDetalheAsync(resultado.Value);
        ordemServico.Should().NotBeNull();
        ordemServico!.Fotos.Should().ContainSingle();
        ordemServico.HashIntegridade.Should().NotBeNullOrWhiteSpace();

        var caminhoFisico = Path.Combine(_pastaTemporaria, "anexos", ordemServico.Fotos.First().CaminhoArmazenamento);
        var bytesEmDisco = await File.ReadAllBytesAsync(caminhoFisico);
        bytesEmDisco.Should().NotBeEquivalentTo(conteudoFoto, "o anexo precisa estar criptografado em disco");

        var decriptografado = await _armazenamento.AbrirDecriptografadoAsync(ordemServico.Fotos.First().CaminhoArmazenamento);
        decriptografado.Should().BeEquivalentTo(conteudoFoto);
    }

    [Fact]
    public async Task CriarComNumeroDuplicado_DeveFalhar()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
        var servico = CriarServico(contexto);

        (await servico.CriarAsync(NovaOrdemDto("OS-2026-9002"))).IsSuccess.Should().BeTrue();
        var duplicada = await servico.CriarAsync(NovaOrdemDto("OS-2026-9002"));

        duplicada.IsSuccess.Should().BeFalse();
        duplicada.Error.Should().ContainEquivalentOf("já existe");
    }

    [Fact]
    public async Task AlterarSituacao_DeveRegistrarEventoNaTimeline()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
        var servico = CriarServico(contexto);

        var criada = await servico.CriarAsync(NovaOrdemDto("OS-2026-9003"));
        await servico.AlterarSituacaoAsync(criada.Value, SituacaoOS.Concluida);

        var ordemServico = await servico.ObterDetalheAsync(criada.Value);
        ordemServico!.Situacao.Should().Be(SituacaoOS.Concluida);
        ordemServico.Timeline.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task Reagendar_DeveAtualizarDataEHora()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
        var servico = CriarServico(contexto);

        var criada = await servico.CriarAsync(NovaOrdemDto("OS-2026-9004"));
        var novaData = new DateOnly(2026, 9, 15);

        await servico.ReagendarAsync(criada.Value, novaData, new TimeOnly(14, 30));

        var ordemServico = await servico.ObterDetalheAsync(criada.Value);
        ordemServico!.Data.Should().Be(novaData);
        ordemServico.Hora.Should().Be(new TimeOnly(14, 30));
    }

    [Fact]
    public async Task Excluir_DeveRemoverRegistroEArquivosDoDisco()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
        var servico = CriarServico(contexto);

        var criada = await servico.CriarAsync(
            NovaOrdemDto("OS-2026-9005"),
            [new NovoArquivoDto("doc.pdf", "application/pdf", Encoding.UTF8.GetBytes("x"), TipoArquivo.Anexo)]);

        var ordemServico = await servico.ObterDetalheAsync(criada.Value);
        var caminhoFisico = Path.Combine(_pastaTemporaria, "anexos", ordemServico!.Anexos.First().CaminhoArmazenamento);
        File.Exists(caminhoFisico).Should().BeTrue();

        await servico.ExcluirAsync(criada.Value);

        (await servico.ObterDetalheAsync(criada.Value)).Should().BeNull();
        File.Exists(caminhoFisico).Should().BeFalse("os anexos precisam ser apagados junto com a OS");
    }

    [Fact]
    public async Task SugerirProximoNumero_DeveContinuarSequenciaDoAno()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
        var servico = CriarServico(contexto);

        var primeiro = await servico.SugerirProximoNumeroAsync();
        primeiro.Should().StartWith($"OS-{DateTime.Today.Year}-");

        await servico.CriarAsync(NovaOrdemDto(primeiro));
        var segundo = await servico.SugerirProximoNumeroAsync();

        segundo.Should().NotBe(primeiro);
    }

    [Fact]
    public async Task Buscar_DeveFiltrarPorTermoESituacao()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
        var servico = CriarServico(contexto);

        await servico.CriarAsync(NovaOrdemDto("OS-2026-9010") with { Empresa = "Padaria Central Ltda" });
        await servico.CriarAsync(NovaOrdemDto("OS-2026-9011") with { Empresa = "Metalúrgica Sul SA" });

        var porTermo = await servico.BuscarAsync(new FiltroOrdemServicoDto { Termo = "Padaria" });
        porTermo.Should().ContainSingle();

        var porSituacao = await servico.BuscarAsync(new FiltroOrdemServicoDto { Situacao = SituacaoOS.Agendada });
        porSituacao.Should().HaveCount(2);
    }

    private static CriarOrdemServicoDto NovaOrdemDto(string numero) => new(
        numero, "Empresa Teste Ltda", "11.444.777/0001-61", "Rua Exemplo, 123", "São Paulo",
        "João da Silva", new DateOnly(2026, 8, 10), new TimeOnly(9, 0),
        TipoAuditoriaSeed.AuditoriaFiscal, null, null, null);

    private OrdemServicoService CriarServico(AuditorFiscalDbContext contexto) => new(
        new UnitOfWork(contexto),
        new CriarOrdemServicoDtoValidator(),
        new AtualizarOrdemServicoDtoValidator(),
        new Sha256HashService(),
        _armazenamento,
        new SystemClock());

    private AuditorFiscalDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<AuditorFiscalDbContext>()
            .UseSqlite(SqliteConnectionStringFactory.Criar(_caminhoBanco, _chave))
            .Options;

        return new AuditorFiscalDbContext(options);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (Directory.Exists(_pastaTemporaria))
                Directory.Delete(_pastaTemporaria, recursive: true);
        }
        catch (IOException)
        {
            // Limpeza best-effort; handles do SQLite podem demorar a liberar.
        }
    }
}
