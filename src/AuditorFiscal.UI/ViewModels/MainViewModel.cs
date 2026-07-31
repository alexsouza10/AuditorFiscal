using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuditorFiscal.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IThemeService _tema;

    [ObservableProperty] private ViewModelBase? _currentPage;
    [ObservableProperty] private string _iconeTema = "🌙";

    public MainViewModel(INavigationService navigation, IThemeService tema)
    {
        _navigation = navigation;
        _tema = tema;

        navigation.CurrentPageChanged += () => CurrentPage = navigation.CurrentPage;
        navigation.IrParaInicio();
        AtualizarIconeTema(tema.TemaAtual);
    }

    [RelayCommand]
    private void AlternarTema() => AtualizarIconeTema(_tema.Alternar());

    [RelayCommand]
    private void Inicio() => _navigation.IrParaInicio();

    [RelayCommand]
    private void NovaOrdemServico() => _navigation.NavegarPara<OrdemServicoFormViewModel>();

    [RelayCommand]
    private void Visualizar() => _navigation.NavegarPara<GanttViewModel>();

    [RelayCommand]
    private void BancoDados() => _navigation.NavegarPara<BancoDadosViewModel>();

    [RelayCommand]
    private void Configuracoes() => _navigation.NavegarPara<ConfiguracoesViewModel>();

    [RelayCommand]
    private void Dashboard() => _navigation.NavegarPara<DashboardViewModel>();

    [RelayCommand]
    private void Voltar() => _navigation.Voltar();

    private void AtualizarIconeTema(TemaAplicacao tema) =>
        IconeTema = tema == TemaAplicacao.Escuro ? "☀" : "🌙";
}
