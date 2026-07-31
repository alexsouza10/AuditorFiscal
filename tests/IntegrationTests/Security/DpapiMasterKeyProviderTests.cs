using AuditorFiscal.Infrastructure.Security;
using AwesomeAssertions;

namespace IntegrationTests.Security;

public class DpapiMasterKeyProviderTests : IDisposable
{
    private readonly string _diretorioTemporario = Path.Combine(Path.GetTempPath(), $"auditorfiscal_dpapi_{Guid.NewGuid():N}");

    [Fact]
    public void ObterChave_PrimeiraChamada_DeveCriarEProtegerChaveEmDisco()
    {
        var provider = new DpapiMasterKeyProvider(_diretorioTemporario);

        var chave = provider.ObterChave();

        chave.Should().HaveCount(32);

        var arquivoChave = Path.Combine(_diretorioTemporario, "master.key");
        File.Exists(arquivoChave).Should().BeTrue();

        var conteudoEmDisco = File.ReadAllBytes(arquivoChave);
        conteudoEmDisco.Should().NotBeEquivalentTo(chave, "a chave em disco deve estar protegida (DPAPI), nunca em texto puro");
    }

    [Fact]
    public void ObterChave_ChamadasRepetidas_DeveRetornarMesmaChave()
    {
        var provider = new DpapiMasterKeyProvider(_diretorioTemporario);

        var chave1 = provider.ObterChave();
        var chave2 = provider.ObterChave();

        chave1.Should().BeEquivalentTo(chave2);
    }

    [Fact]
    public void ObterChave_NovaInstanciaMesmoDiretorio_DeveRecuperarChavePersistida()
    {
        var chaveOriginal = new DpapiMasterKeyProvider(_diretorioTemporario).ObterChave();

        var novaInstancia = new DpapiMasterKeyProvider(_diretorioTemporario);
        var chaveRecuperada = novaInstancia.ObterChave();

        chaveRecuperada.Should().BeEquivalentTo(chaveOriginal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_diretorioTemporario))
            Directory.Delete(_diretorioTemporario, recursive: true);
    }
}
