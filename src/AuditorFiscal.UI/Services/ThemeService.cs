using AuditorFiscal.Application.Interfaces.Services;
using Avalonia.Styling;

namespace AuditorFiscal.UI.Services;

public interface IThemeService
{
    TemaAplicacao TemaAtual { get; }
    void Aplicar(TemaAplicacao tema);
    TemaAplicacao Alternar();
}

public class ThemeService(IPreferencesService preferencias) : IThemeService
{
    public TemaAplicacao TemaAtual => preferencias.Atual.Tema;

    public void Aplicar(TemaAplicacao tema)
    {
        if (global::Avalonia.Application.Current is { } app)
            app.RequestedThemeVariant = Converter(tema);

        preferencias.Salvar(preferencias.Atual with { Tema = tema });
    }

    /// <summary>
    /// Alterna apenas entre Claro e Escuro: partindo de "Sistema", vai para o oposto do
    /// que está sendo exibido, que é o que o usuário espera ao clicar no botão de tema.
    /// </summary>
    public TemaAplicacao Alternar()
    {
        var proximo = TemaAtual switch
        {
            TemaAplicacao.Claro => TemaAplicacao.Escuro,
            TemaAplicacao.Escuro => TemaAplicacao.Claro,
            _ => EstaEscuroAgora() ? TemaAplicacao.Claro : TemaAplicacao.Escuro
        };

        Aplicar(proximo);
        return proximo;
    }

    private static bool EstaEscuroAgora() =>
        global::Avalonia.Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    private static ThemeVariant Converter(TemaAplicacao tema) => tema switch
    {
        TemaAplicacao.Claro => ThemeVariant.Light,
        TemaAplicacao.Escuro => ThemeVariant.Dark,
        _ => ThemeVariant.Default
    };
}
