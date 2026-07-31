using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace AuditorFiscal.UI.Services;

internal static class JanelaAtual
{
    public static Window Obter()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } janela })
            return janela;

        throw new InvalidOperationException("Janela principal não disponível.");
    }
}
