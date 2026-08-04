using System.Security.Cryptography;
using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Application.OrdensServico;
using AuditorFiscal.Application.OrdensServico.Busca;
using AuditorFiscal.Application.OrdensServico.Dtos;
using AuditorFiscal.Application.OrdensServico.Validators;
using AuditorFiscal.Domain.Enums;
using AuditorFiscal.Infrastructure.Security;
using AuditorFiscal.Persistence;
using AuditorFiscal.Persistence.Security;
using AuditorFiscal.Shared;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Persistence;

/// <summary>
/// Cobre a busca livre via FTS5 (tabela virtual sincronizada por gatilhos, ver migration
/// AdicionarBuscaFts) e os filtros da query DSL (<see cref="ConsultaOrdemServicoParser"/>)
/// contra um banco SQLCipher real — é o único jeito de pegar um erro de sintaxe FTS5, uma
/// falha de gatilho ou uma conversão de Guid quebrada, já que nada disso é traduzido pelo
/// LINQ provider do EF Core e passa despercebido em testes puramente in-memory.
/// </summary>
public class BuscaOrdemServicoTests : IDisposable
{
    private sealed class ChaveFixaProvider(byte[] chave) : IMasterKeyProvider
    {
        public byte[] ObterChave() => chave;
    }

    private readonly string _pastaTemporaria = Path.Combine(Path.GetTempPath(), $"af_busca_{Guid.NewGuid():N}");
    private readonly byte[] _chave = RandomNumberGenerator.GetBytes(32);
    private readonly string _caminhoBanco;
    private readonly IAttachmentStorageService _armazenamento;

    public BuscaOrdemServicoTests()
    {
        Directory.CreateDirectory(_pastaTemporaria);
        _caminhoBanco = Path.Combine(_pastaTemporaria, "teste.db");

        var chaveProvider = new ChaveFixaProvider(_chave);
        var hash = new Sha256HashService();
        _armazenamento = new FileSystemAttachmentStorageService(
            new AesGcmEncryptionService(chaveProvider), hash, Path.Combine(_pastaTemporaria, "anexos"));
    }

    [Fact]
    public async Task Buscar_PorPalavraSomenteNasObservacoes_DeveEncontrarViaFts()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
        var servico = CriarServico(contexto);

        await servico.CriarAsync(NovaOrdemDto("OS-2026-8001") with
        {
            Empresa = "Metalúrgica Sul SA",
            Observacoes = "Constatada irregularidade grave no uso de EPI durante a inspeção."
        });
        await servico.CriarAsync(NovaOrdemDto("OS-2026-8002") with { Empresa = "Padaria Central Ltda" });

        var resultado = await servico.BuscarAsync(ConsultaOrdemServicoParser.Interpretar("irregularidade"));

        resultado.Should().ContainSingle();
        resultado[0].Empresa.Should().Be("Metalúrgica Sul SA");
    }

    [Fact]
    public async Task Buscar_AposAtualizarObservacoes_DeveRefletirNovoConteudoNoIndiceFts()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
        var servico = CriarServico(contexto);

        var criada = await servico.CriarAsync(NovaOrdemDto("OS-2026-8003") with { Observacoes = "texto original" });

        var antes = await servico.BuscarAsync(ConsultaOrdemServicoParser.Interpretar("original"));
        antes.Should().ContainSingle();

        var dto = AtualizarDto(criada.Value, "OS-2026-8003", observacoes: "texto revisado após vistoria");
        await servico.AtualizarAsync(dto);

        var peloTextoAntigo = await servico.BuscarAsync(ConsultaOrdemServicoParser.Interpretar("original"));
        var peloTextoNovo = await servico.BuscarAsync(ConsultaOrdemServicoParser.Interpretar("vistoria"));

        peloTextoAntigo.Should().BeEmpty("o gatilho de update deveria ter removido o texto antigo do índice FTS");
        peloTextoNovo.Should().ContainSingle();
    }

    [Fact]
    public async Task Buscar_AposExcluir_NaoDeveMaisAparecerNaBuscaLivre()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
        var servico = CriarServico(contexto);

        var criada = await servico.CriarAsync(NovaOrdemDto("OS-2026-8004") with { Observacoes = "achado exclusivo de vazamento" });
        (await servico.BuscarAsync(ConsultaOrdemServicoParser.Interpretar("vazamento"))).Should().ContainSingle();

        await servico.ExcluirAsync(criada.Value);

        (await servico.BuscarAsync(ConsultaOrdemServicoParser.Interpretar("vazamento"))).Should().BeEmpty();
    }

    [Fact]
    public async Task Buscar_ComTokenEmpresa_DeveFiltrarPorContemSemPrecisarDoTextoExato()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
        var servico = CriarServico(contexto);

        await servico.CriarAsync(NovaOrdemDto("OS-2026-8005") with { Empresa = "Auto Peças Toyota Ltda" });
        await servico.CriarAsync(NovaOrdemDto("OS-2026-8006") with { Empresa = "Padaria Central Ltda" });

        var resultado = await servico.BuscarAsync(ConsultaOrdemServicoParser.Interpretar("empresa:toyota"));

        resultado.Should().ContainSingle();
        resultado[0].Numero.Should().Be("OS-2026-8005");
    }

    [Fact]
    public async Task Buscar_ComTokenAtrasadas_DeveTrazerApenasOsComDataFinalNoPassadoEAindaAtivas()
    {
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
        var servico = CriarServico(contexto);

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var vencida = await servico.CriarAsync(NovaOrdemDto("OS-2026-8007", dataFinal: hoje.AddDays(-5)));
        await servico.CriarAsync(NovaOrdemDto("OS-2026-8008", dataFinal: hoje.AddDays(30)));

        var resultado = await servico.BuscarAsync(ConsultaOrdemServicoParser.Interpretar("atrasadas"));

        resultado.Should().ContainSingle();
        resultado[0].Id.Should().Be(vencida.Value);
    }

    /// <summary>
    /// Datas relativas a "hoje" (não fixas em 2026): o teste de "atrasadas" precisa poder pedir
    /// uma <paramref name="dataFinal"/> no passado sem violar a ordem crescente exigida pelo
    /// validator, o que datas fixas não permitiriam de forma confiável.
    /// </summary>
    private static CriarOrdemServicoDto NovaOrdemDto(string numero, DateOnly? dataFinal = null)
    {
        var final = dataFinal ?? DateOnly.FromDateTime(DateTime.Today).AddDays(45);
        var recebimento = final.AddDays(-45);

        return new(
            numero, "Empresa Teste Ltda", "11.444.777/0001-61", "Rua Exemplo, 123", "São Paulo",
            "João da Silva", TipoFiscalizacao.Direta,
            recebimento, recebimento.AddDays(2), recebimento.AddDays(8), recebimento.AddDays(15),
            recebimento.AddDays(26), recebimento.AddDays(36), final,
            null, null, null);
    }

    private static AtualizarOrdemServicoDto AtualizarDto(Guid id, string numero, string observacoes) => new(
        id, numero, "Empresa Teste Ltda", "11.444.777/0001-61", "Rua Exemplo, 123", "São Paulo",
        "João da Silva", TipoFiscalizacao.Direta,
        new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 12), new DateOnly(2026, 8, 18), new DateOnly(2026, 8, 25),
        new DateOnly(2026, 9, 5), new DateOnly(2026, 9, 15), new DateOnly(2026, 9, 25),
        observacoes, null, null, false, null);

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
