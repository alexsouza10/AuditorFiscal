using AuditorFiscal.Application.OrdensServico;
using AuditorFiscal.Domain.Enums;
using AuditorFiscal.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuditorFiscal.UI.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly OrdemServicoService _ordemServicoService;

    [ObservableProperty] private int _totalHoje;
    [ObservableProperty] private int _totalSemana;
    [ObservableProperty] private int _totalPendentes;
    [ObservableProperty] private string _resumo = "Selecione uma opção para começar";

    public HomeViewModel(INavigationService navigation, OrdemServicoService ordemServicoService)
    {
        _navigation = navigation;
        _ordemServicoService = ordemServicoService;
        _ = CarregarResumoAsync();
    }

    [RelayCommand]
    private void NovaOrdemServico() => _navigation.NavegarPara<OrdemServicoFormViewModel>();

    [RelayCommand]
    private void Agenda() => _navigation.NavegarPara<AgendaViewModel>();

    [RelayCommand]
    private void BancoDados() => _navigation.NavegarPara<BancoDadosViewModel>();

    private async Task CarregarResumoAsync()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var inicioSemana = hoje.AddDays(-(int)hoje.DayOfWeek);

        var daSemana = await _ordemServicoService.ObterPorPeriodoAsync(inicioSemana, inicioSemana.AddDays(6));

        TotalHoje = daSemana.Count(o => o.Data == hoje);
        TotalSemana = daSemana.Count;
        TotalPendentes = daSemana.Count(o => o.Situacao is SituacaoOS.Agendada or SituacaoOS.EmAndamento);

        Resumo = TotalHoje switch
        {
            0 => "Nenhuma auditoria agendada para hoje",
            1 => "1 auditoria agendada para hoje",
            _ => $"{TotalHoje} auditorias agendadas para hoje"
        };
    }
}
