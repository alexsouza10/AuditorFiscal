using AuditorFiscal.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AuditorFiscal.UI.Services;

public class NavigationService(IServiceProvider serviceProvider) : INavigationService
{
    private readonly Stack<ViewModelBase> _historico = new();

    public ViewModelBase? CurrentPage { get; private set; }
    public bool PodeVoltar => _historico.Count > 0;

    public event Action? CurrentPageChanged;

    public TViewModel Resolver<TViewModel>() where TViewModel : ViewModelBase =>
        serviceProvider.GetRequiredService<TViewModel>();

    public void NavegarPara<TViewModel>() where TViewModel : ViewModelBase =>
        NavegarPara(Resolver<TViewModel>());

    public void NavegarPara(ViewModelBase viewModel)
    {
        if (CurrentPage is not null)
            _historico.Push(CurrentPage);

        DefinirPagina(viewModel);
    }

    public void Voltar()
    {
        if (_historico.Count == 0)
            return;

        DefinirPagina(_historico.Pop());
    }

    public void IrParaInicio()
    {
        _historico.Clear();
        DefinirPagina(Resolver<HomeViewModel>());
    }

    private void DefinirPagina(ViewModelBase viewModel)
    {
        // ViewModels com timer/recursos (ex.: auto save do formulário) precisam parar ao sair da tela.
        if (CurrentPage is IDisposable descartavel && !ReferenceEquals(CurrentPage, viewModel) && !_historico.Contains(CurrentPage))
            descartavel.Dispose();

        CurrentPage = viewModel;
        CurrentPageChanged?.Invoke();
    }
}
