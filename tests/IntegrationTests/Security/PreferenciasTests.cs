using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Infrastructure.Preferences;
using AwesomeAssertions;

namespace IntegrationTests.Security;

public class PreferenciasTests : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), $"af_pref_{Guid.NewGuid():N}");

    [Fact]
    public void Preferencias_SemArquivo_DeveUsarPadroes()
    {
        var servico = new JsonPreferencesService(_pasta);

        servico.Atual.Tema.Should().Be(TemaAplicacao.Sistema);
        servico.Atual.BackupAutomatico.Should().BeTrue();
    }

    [Fact]
    public void Salvar_DevePersistirEntreInstancias()
    {
        new JsonPreferencesService(_pasta).Salvar(new Preferencias { Tema = TemaAplicacao.Escuro, IniciarComWindows = true });

        var recarregado = new JsonPreferencesService(_pasta);

        recarregado.Atual.Tema.Should().Be(TemaAplicacao.Escuro);
        recarregado.Atual.IniciarComWindows.Should().BeTrue();
    }

    [Fact]
    public void Carregar_ComArquivoCorrompido_DeveVoltarAosPadroesSemLancar()
    {
        Directory.CreateDirectory(_pasta);
        File.WriteAllText(Path.Combine(_pasta, "preferences.json"), "{ isto não é json válido");

        var servico = new JsonPreferencesService(_pasta);

        servico.Atual.Tema.Should().Be(TemaAplicacao.Sistema);
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
