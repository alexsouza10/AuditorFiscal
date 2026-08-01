using System.Collections.ObjectModel;
using System.Globalization;
using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Application.OrdensServico;
using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;
using AuditorFiscal.UI.Messaging;
using AuditorFiscal.UI.Services;
using AuditorFiscal.UI.ViewModels.Gantt;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace AuditorFiscal.UI.ViewModels;

/// <summary>
/// Cronograma GANTT (CLAUDE V2): substitui a agenda tradicional por uma linha do tempo
/// em grade, dividida por meses, com uma barra por O.S. e uma linha vertical de "HOJE".
/// </summary>
public partial class GanttViewModel : ViewModelBase, IDisposable
{
    private const int PeriodoPadraoMeses = 12;
    private static readonly CultureInfo CulturaPtBr = new("pt-BR");

    private readonly OrdemServicoService _ordemServicoService;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly IPdfExportService _pdfExport;
    private readonly IFileDialogService _fileDialog;
    private readonly DispatcherTimer _debounceBusca;

    private DateOnly _inicioJanela;
    private DateOnly _fimJanela;
    private int _mesesVisiveis = PeriodoPadraoMeses;

    [ObservableProperty] private string _tituloPeriodo = string.Empty;
    [ObservableProperty] private LinhaGanttViewModel? _selecionada;
    [ObservableProperty] private SituacaoOS _situacaoEdicao;
    [ObservableProperty] private string? _mensagemStatus;
    [ObservableProperty] private double _posicaoLinhaHoje;
    [ObservableProperty] private bool _hojeVisivel;
    [ObservableProperty] private string? _filtroTexto;
    [ObservableProperty] private DateTime? _filtroInicio;
    [ObservableProperty] private DateTime? _filtroFim;
    [ObservableProperty] private int _periodoSelecionado = PeriodoPadraoMeses;

    public ObservableCollection<MesGanttViewModel> Meses { get; } = [];
    public ObservableCollection<LinhaGanttViewModel> Linhas { get; } = [];
    public IReadOnlyList<SituacaoOS> SituacoesDisponiveis { get; } = Enum.GetValues<SituacaoOS>();

    public bool TemSelecao => Selecionada is not null;

    public GanttViewModel(
        OrdemServicoService ordemServicoService,
        INavigationService navigation,
        IDialogService dialogs,
        IPdfExportService pdfExport,
        IFileDialogService fileDialog)
    {
        _ordemServicoService = ordemServicoService;
        _navigation = navigation;
        _dialogs = dialogs;
        _pdfExport = pdfExport;
        _fileDialog = fileDialog;

        var inicioMesAtual = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        _inicioJanela = inicioMesAtual.AddMonths(-1);
        _fimJanela = inicioMesAtual.AddMonths(_mesesVisiveis - 1).AddMonths(1).AddDays(-1);

        _debounceBusca = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _debounceBusca.Tick += async (_, _) =>
        {
            _debounceBusca.Stop();
            await CarregarAsync();
        };

        WeakReferenceMessenger.Default.Register<OrdemServicoAlteradaMessage>(this, async (_, _) => await CarregarAsync());

        _ = CarregarAsync();
    }

    partial void OnFiltroTextoChanged(string? value)
    {
        _debounceBusca.Stop();
        _debounceBusca.Start();
    }

    [RelayCommand]
    private async Task PeriodoAnteriorAsync()
    {
        _inicioJanela = _inicioJanela.AddMonths(-_mesesVisiveis);
        _fimJanela = _fimJanela.AddMonths(-_mesesVisiveis);
        await CarregarAsync();
    }

    [RelayCommand]
    private async Task ProximoPeriodoAsync()
    {
        _inicioJanela = _inicioJanela.AddMonths(_mesesVisiveis);
        _fimJanela = _fimJanela.AddMonths(_mesesVisiveis);
        await CarregarAsync();
    }

    [RelayCommand]
    private async Task HojeAsync()
    {
        var inicioMesAtual = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        _inicioJanela = inicioMesAtual.AddMonths(-1);
        _fimJanela = inicioMesAtual.AddMonths(_mesesVisiveis - 1).AddMonths(1).AddDays(-1);
        await CarregarAsync();
    }

    /// <summary>Botões de período rápido (3/6/12 meses) — janela recomeça a partir do mês atual.</summary>
    [RelayCommand]
    private async Task DefinirPeriodoAsync(string mesesTexto)
    {
        var meses = int.Parse(mesesTexto, CultureInfo.InvariantCulture);
        _mesesVisiveis = meses;
        PeriodoSelecionado = meses;
        FiltroInicio = null;
        FiltroFim = null;

        var inicioMesAtual = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        _inicioJanela = inicioMesAtual;
        _fimJanela = inicioMesAtual.AddMonths(meses).AddDays(-1);
        await CarregarAsync();
    }

    /// <summary>Aplica um intervalo de datas escolhido livremente no calendário, sobrepondo os botões de período.</summary>
    [RelayCommand]
    private async Task AplicarPeriodoPersonalizadoAsync()
    {
        if (FiltroInicio is null || FiltroFim is null || FiltroInicio > FiltroFim)
        {
            MensagemStatus = "Informe um período válido (início antes do fim).";
            return;
        }

        _inicioJanela = DateOnly.FromDateTime(FiltroInicio.Value);
        _fimJanela = DateOnly.FromDateTime(FiltroFim.Value);
        _mesesVisiveis = Math.Max(1, (_fimJanela.Year - _inicioJanela.Year) * 12 + _fimJanela.Month - _inicioJanela.Month + 1);
        PeriodoSelecionado = 0;
        await CarregarAsync();
    }

    [RelayCommand]
    private void SelecionarLinha(LinhaGanttViewModel linha)
    {
        if (Selecionada is not null)
            Selecionada.Selecionada = false;

        Selecionada = linha;
        linha.Selecionada = true;
        SituacaoEdicao = linha.OrdemServico.Situacao;

        OnPropertyChanged(nameof(TemSelecao));
    }

    [RelayCommand]
    private void NovaOrdem() => _navigation.NavegarPara<OrdemServicoFormViewModel>();

    [RelayCommand]
    private async Task AbrirDetalheAsync()
    {
        if (Selecionada is null)
            return;

        var formulario = _navigation.Resolver<OrdemServicoFormViewModel>();
        await formulario.CarregarParaEdicaoAsync(Selecionada.Id);
        _navigation.NavegarPara(formulario);
    }

    [RelayCommand]
    private async Task AbrirHistoricoAsync()
    {
        if (Selecionada is null)
            return;

        var formulario = _navigation.Resolver<OrdemServicoFormViewModel>();
        await formulario.CarregarParaEdicaoAsync(Selecionada.Id);
        formulario.AbaSelecionada = 1;
        _navigation.NavegarPara(formulario);
    }

    [RelayCommand]
    private async Task AplicarSituacaoAsync()
    {
        if (Selecionada is null)
            return;

        var resultado = await _ordemServicoService.AlterarSituacaoAsync(Selecionada.Id, SituacaoEdicao);
        MensagemStatus = resultado.IsSuccess ? "Situação atualizada." : resultado.Error;
        await CarregarAsync(Selecionada.Id);
        WeakReferenceMessenger.Default.Send(new OrdemServicoAlteradaMessage());
    }

    [RelayCommand]
    private async Task AlternarFavoritoAsync()
    {
        if (Selecionada is null)
            return;

        await _ordemServicoService.AlternarFavoritoAsync(Selecionada.Id);
        await CarregarAsync(Selecionada.Id);
        WeakReferenceMessenger.Default.Send(new OrdemServicoAlteradaMessage());
    }

    [RelayCommand]
    private async Task ExcluirAsync()
    {
        if (Selecionada is null)
            return;

        if (!await _dialogs.ConfirmarAsync("Excluir ordem de serviço",
                $"Excluir a OS {Selecionada.Numero} ({Selecionada.Empresa}) e seus anexos?", "Excluir"))
            return;

        await _ordemServicoService.ExcluirAsync(Selecionada.Id);
        MensagemStatus = "Ordem de serviço excluída.";
        await CarregarAsync();
        WeakReferenceMessenger.Default.Send(new OrdemServicoAlteradaMessage());
    }

    [RelayCommand]
    private async Task AdiarUmaSemanaAsync()
    {
        if (Selecionada is null)
            return;

        await _ordemServicoService.AdiarAsync(Selecionada.Id, 7);
        MensagemStatus = "Cronograma adiado em 7 dias.";
        await CarregarAsync(Selecionada.Id);
        WeakReferenceMessenger.Default.Send(new OrdemServicoAlteradaMessage());
    }

    [RelayCommand]
    private async Task ExportarPdfAsync()
    {
        if (Selecionada is null)
            return;

        var ordemServico = await _ordemServicoService.ObterDetalheAsync(Selecionada.Id);
        if (ordemServico is null)
            return;

        var destino = await _fileDialog.SalvarComoAsync($"OS-{ordemServico.Numero}", "Documento PDF", "pdf");
        if (destino is null)
            return;

        await _pdfExport.ExportarOrdemServicoAsync(ordemServico, destino);
        MensagemStatus = "PDF exportado.";
    }

    [RelayCommand]
    private void FecharDetalhe()
    {
        if (Selecionada is not null)
            Selecionada.Selecionada = false;

        Selecionada = null;
        OnPropertyChanged(nameof(TemSelecao));
    }

    [RelayCommand]
    private void Voltar() => _navigation.IrParaInicio();

    private async Task CarregarAsync(Guid? manterSelecionado = null)
    {
        var ordens = await _ordemServicoService.ObterPorPeriodoAsync(_inicioJanela, _fimJanela);

        if (!string.IsNullOrWhiteSpace(FiltroTexto))
        {
            var termo = FiltroTexto.Trim();
            var termoDigitos = new string(termo.Where(char.IsDigit).ToArray());

            ordens = ordens.Where(o =>
                    o.Numero.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    o.Empresa.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    (termoDigitos.Length > 0 && o.Cnpj.Numero.Contains(termoDigitos)))
                .ToList();
        }

        TituloPeriodo = MontarTitulo(_inicioJanela, _fimJanela);

        Meses.Clear();
        var cursor = _inicioJanela;
        while (cursor <= _fimJanela)
        {
            var fimMes = new DateOnly(cursor.Year, cursor.Month, DateTime.DaysInMonth(cursor.Year, cursor.Month));
            if (fimMes > _fimJanela)
                fimMes = _fimJanela;

            Meses.Add(new MesGanttViewModel(cursor, fimMes, _inicioJanela, _fimJanela));
            cursor = cursor.AddMonths(1);
            cursor = new DateOnly(cursor.Year, cursor.Month, 1);
        }

        var totalDiasJanela = Math.Max(1, _fimJanela.DayNumber - _inicioJanela.DayNumber);
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        HojeVisivel = hoje >= _inicioJanela && hoje <= _fimJanela;
        PosicaoLinhaHoje = (hoje.DayNumber - _inicioJanela.DayNumber) / (double)totalDiasJanela;

        Linhas.Clear();
        LinhaGanttViewModel? selecionar = null;

        foreach (var ordemServico in ordens.OrderBy(o => o.RecebimentoSfit))
        {
            var linha = new LinhaGanttViewModel(ordemServico, _inicioJanela, _fimJanela);
            Linhas.Add(linha);

            if (manterSelecionado == ordemServico.Id)
                selecionar = linha;
        }

        Selecionada = null;
        OnPropertyChanged(nameof(TemSelecao));

        if (selecionar is not null)
            SelecionarLinha(selecionar);
    }

    private static string MontarTitulo(DateOnly inicio, DateOnly fim)
    {
        var mesInicio = Capitalizar(inicio.ToDateTime(TimeOnly.MinValue).ToString("MMM", CulturaPtBr));
        var mesFim = Capitalizar(fim.ToDateTime(TimeOnly.MinValue).ToString("MMM", CulturaPtBr));
        return $"{mesInicio}/{inicio.Year} — {mesFim}/{fim.Year}";
    }

    private static string Capitalizar(string texto) =>
        string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0], CulturaPtBr) + texto[1..].TrimEnd('.');

    public void Dispose()
    {
        _debounceBusca.Stop();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
