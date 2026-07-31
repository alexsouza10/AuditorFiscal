using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.ValueObjects;
using AuditorFiscal.Persistence;
using AuditorFiscal.Persistence.Configurations;
using AuditorFiscal.Persistence.Security;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Persistence;

/// <summary>
/// Spike de segurança: prova que o banco SQLite realmente está criptografado
/// (SQLCipher) e não apenas "funciona por coincidência" sem chave nenhuma.
/// </summary>
public class SqliteEncryptionTests : IDisposable
{
    private readonly string _caminhoBanco = Path.Combine(Path.GetTempPath(), $"auditorfiscal_{Guid.NewGuid():N}.db");

    [Fact]
    public async Task EscreverELer_ComMesmaChave_DeveFazerRoundTripCorretamente()
    {
        var chave = RandomKey();

        await using (var contexto = CriarContexto(chave))
        {
            await contexto.Database.MigrateAsync();

            var ordemServico = new OrdemServico(
                numero: "OS-2026-0042",
                empresa: "Empresa Teste Ltda",
                cnpj: Cnpj.Criar("11.444.777/0001-61"),
                endereco: "Rua Exemplo, 123",
                cidade: "São Paulo",
                responsavel: "João da Silva",
                data: new DateOnly(2026, 8, 1),
                hora: new TimeOnly(9, 0),
                tipoAuditoriaId: TipoAuditoriaSeed.AuditoriaFiscal,
                momento: DateTimeOffset.UtcNow,
                coordenada: new Coordenada(-23.55, -46.63));

            contexto.OrdensServico.Add(ordemServico);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(chave))
        {
            var ordemServico = await contexto.OrdensServico.SingleAsync();

            ordemServico.Numero.Should().Be("OS-2026-0042");
            ordemServico.Cnpj.Numero.Should().Be("11444777000161");
            ordemServico.Coordenada.Should().NotBeNull();
            ordemServico.Coordenada!.Latitude.Should().Be(-23.55);
        }
    }

    [Fact]
    public async Task AbrirBanco_ComChaveErrada_DeveFalhar()
    {
        var chaveCorreta = RandomKey();
        var chaveErrada = RandomKey();

        await using (var contexto = CriarContexto(chaveCorreta))
        {
            await contexto.Database.MigrateAsync();
        }

        await using var contextoComChaveErrada = CriarContexto(chaveErrada);

        var acao = async () => await contextoComChaveErrada.OrdensServico.ToListAsync();

        await acao.Should().ThrowAsync<SqliteException>();
    }

    private AuditorFiscalDbContext CriarContexto(byte[] chave)
    {
        var connectionString = SqliteConnectionStringFactory.Criar(_caminhoBanco, chave);
        var options = new DbContextOptionsBuilder<AuditorFiscalDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new AuditorFiscalDbContext(options);
    }

    private static byte[] RandomKey() => System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(_caminhoBanco))
                File.Delete(_caminhoBanco);
        }
        catch (IOException)
        {
            // Limpeza best-effort: o arquivo temporário de teste pode ainda estar
            // com handle preso pelo pool de conexões; não afeta a validade do teste.
        }
    }
}
