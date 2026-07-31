using AuditorFiscal.UI.ViewModels;

namespace AuditorFiscal.UI.Services;

public interface INavigationService
{
    ViewModelBase? CurrentPage { get; }
    bool PodeVoltar { get; }

    event Action? CurrentPageChanged;

    void NavegarPara<TViewModel>() where TViewModel : ViewModelBase;
    void NavegarPara(ViewModelBase viewModel);
    TViewModel Resolver<TViewModel>() where TViewModel : ViewModelBase;
    void Voltar();
    void IrParaInicio();
}
