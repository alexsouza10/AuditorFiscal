using AuditorFiscal.Application.OrdensServico;
using AuditorFiscal.Application.OrdensServico.Dtos;
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

    public string VersaoTexto => $"v{AppVersion.Atual}";

    public HomeViewModel(INavigationService navigation, OrdemServicoService ordemServicoService)
    {
        _navigation = navigation;
        _ordemServicoService = ordemServicoService;
        _ = CarregarResumoAsync();
    }

    [RelayCommand]
    private void NovaOrdemServico() => _navigation.NavegarPara<OrdemServicoFormViewModel>();

    [RelayCommand]
    private void Visualizar() => _navigation.NavegarPara<GanttViewModel>();

    [RelayCommand]
    private void BancoDados() => _navigation.NavegarPara<BancoDadosViewModel>();

    [RelayCommand]
    private void Dashboard() => _navigation.NavegarPara<DashboardViewModel>();

    [RelayCommand]
    private void VerAuditoriasHoje() => _navigation.NavegarPara<GanttViewModel>();

    private async Task CarregarResumoAsync()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var inicioSemana = hoje.AddDays(-(int)hoje.DayOfWeek);

        // "Ativa hoje/na semana" = o intervalo [RecebimentoSfit, DataFinal] cruza a data,
        // não um único campo de data como numa agenda tradicional.
        var daSemana = await _ordemServicoService.ObterPorPeriodoAsync(inicioSemana, inicioSemana.AddDays(6));

        TotalHoje = daSemana.Count(o => o.RecebimentoSfit <= hoje && o.DataFinal >= hoje);
        TotalSemana = daSemana.Count;

        var abertas = await _ordemServicoService.BuscarAsync(new FiltroOrdemServicoDto());
        TotalPendentes = abertas.Count(o => o.Situacao is SituacaoOS.Agendada or SituacaoOS.EmAndamento);

        Resumo = TotalHoje switch
        {
            0 => "Nenhuma auditoria em andamento hoje",
            1 => "1 auditoria em andamento hoje",
            _ => $"{TotalHoje} auditorias em andamento hoje"
        };
    }
}
