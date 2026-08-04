using AuditorFiscal.UI.ViewModels;
using AuditorFiscal.UI.Views;

namespace AuditorFiscal.UI.Services;

/// <summary>Abre a janela de Pesquisa Global (Ctrl+P) como um diálogo modal, no mesmo estilo
/// de construção em código do <see cref="DialogService"/>.</summary>
public class GlobalSearchService(INavigationService navigation) : IGlobalSearchService
{
    public async Task AbrirAsync()
    {
        var viewModel = navigation.Resolver<PesquisaGlobalViewModel>();
        var janela = new PesquisaGlobalWindow { DataContext = viewModel };

        void Fechar() => janela.Close();
        viewModel.RequisitarFechamento += Fechar;

        await janela.ShowDialog(JanelaAtual.Obter());

        viewModel.RequisitarFechamento -= Fechar;
        viewModel.Dispose();
    }
}
