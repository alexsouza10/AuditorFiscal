using System.Collections.ObjectModel;
using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Application.OrdensServico;
using AuditorFiscal.Application.OrdensServico.Dtos;
using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;
using AuditorFiscal.UI.Messaging;
using AuditorFiscal.UI.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace AuditorFiscal.UI.ViewModels;

public partial class BancoDadosViewModel : ViewModelBase, IDisposable
{
    private readonly OrdemServicoService _ordemServicoService;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly IPdfExportService _pdfExport;
    private readonly IExcelExportService _excelExport;
    private readonly IFileDialogService _fileDialog;

    /// <summary>Evita uma consulta ao banco a cada tecla digitada na busca.</summary>
    private readonly DispatcherTimer _debounceBusca;

    [ObservableProperty] private string? _termoBusca;
    [ObservableProperty] private SituacaoOS? _situacaoFiltro;
    [ObservableProperty] private string? _empresaFiltro;
    [ObservableProperty] private bool _somenteFavoritos;
    [ObservableProperty] private OrdemServico? _selecionada;
    [ObservableProperty] private string? _mensagemStatus;
    [ObservableProperty] private int _total;
    [ObservableProperty] private int _totalAgendadas;
    [ObservableProperty] private int _totalEmAndamento;
    [ObservableProperty] private int _totalConcluidas;
    [ObservableProperty] private int _totalFavoritas;

    public ObservableCollection<OrdemServico> Resultados { get; } = [];
    public ObservableCollection<string> Empresas { get; } = [];
    public ObservableCollection<BarraDashboardViewModel> DistribuicaoSituacao { get; } = [];
    public ObservableCollection<BarraDashboardViewModel> DistribuicaoTipo { get; } = [];
    public ObservableCollection<LogInterno> Logs { get; } = [];
    public ObservableCollection<TimelineEvento> TimelineSelecionada { get; } = [];

    public IReadOnlyList<SituacaoOS?> SituacoesFiltro { get; } =
        new SituacaoOS?[] { null }.Concat(Enum.GetValues<SituacaoOS>().Cast<SituacaoOS?>()).ToList();

    public bool TemSelecao => Selecionada is not null;

    public BancoDadosViewModel(
        OrdemServicoService ordemServicoService,
        INavigationService navigation,
        IDialogService dialogs,
        IPdfExportService pdfExport,
        IExcelExportService excelExport,
        IFileDialogService fileDialog)
    {
        _ordemServicoService = ordemServicoService;
        _navigation = navigation;
        _dialogs = dialogs;
        _pdfExport = pdfExport;
        _excelExport = excelExport;
        _fileDialog = fileDialog;

        _debounceBusca = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _debounceBusca.Tick += async (_, _) =>
        {
            _debounceBusca.Stop();
            await BuscarAsync();
        };

        WeakReferenceMessenger.Default.Register<OrdemServicoAlteradaMessage>(this, async (_, _) => await BuscarAsync());

        _ = InicializarAsync();
    }

    partial void OnTermoBuscaChanged(string? value) => ReiniciarDebounce();
    partial void OnSituacaoFiltroChanged(SituacaoOS? value) => ReiniciarDebounce();
    partial void OnEmpresaFiltroChanged(string? value) => ReiniciarDebounce();
    partial void OnSomenteFavoritosChanged(bool value) => ReiniciarDebounce();

    partial void OnSelecionadaChanged(OrdemServico? value)
    {
        OnPropertyChanged(nameof(TemSelecao));
        TimelineSelecionada.Clear();

        if (value is null)
            return;

        foreach (var evento in value.Timeline.OrderByDescending(t => t.OcorridoEm))
            TimelineSelecionada.Add(evento);
    }

    [RelayCommand]
    private async Task BuscarAsync()
    {
        var filtro = new FiltroOrdemServicoDto
        {
            Termo = TermoBusca,
            Situacao = SituacaoFiltro,
            SomenteFavoritos = SomenteFavoritos
        };

        var resultados = await _ordemServicoService.BuscarAsync(filtro);

        if (!string.IsNullOrWhiteSpace(EmpresaFiltro))
            resultados = resultados.Where(o => o.Empresa == EmpresaFiltro).ToList();

        Resultados.Clear();
        foreach (var ordem in resultados)
            Resultados.Add(ordem);

        AtualizarIndicadores(resultados);
        MensagemStatus = $"{resultados.Count} resultado(s).";
    }

    [RelayCommand]
    private async Task LimparFiltrosAsync()
    {
        TermoBusca = null;
        SituacaoFiltro = null;
        EmpresaFiltro = null;
        SomenteFavoritos = false;
        await BuscarAsync();
    }

    [RelayCommand]
    private async Task AbrirAsync()
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
    private async Task ExcluirAsync()
    {
        if (Selecionada is null)
            return;

        if (!await _dialogs.ConfirmarAsync("Excluir ordem de serviço",
                $"Excluir a OS {Selecionada.Numero} ({Selecionada.Empresa}) e seus anexos?", "Excluir"))
            return;

        await _ordemServicoService.ExcluirAsync(Selecionada.Id);
        Selecionada = null;
        await BuscarAsync();
        MensagemStatus = "Ordem de serviço excluída.";
        WeakReferenceMessenger.Default.Send(new OrdemServicoAlteradaMessage());
    }

    [RelayCommand]
    private async Task AlternarFavoritoAsync()
    {
        if (Selecionada is null)
            return;

        await _ordemServicoService.AlternarFavoritoAsync(Selecionada.Id);
        var id = Selecionada.Id;
        await BuscarAsync();
        Selecionada = Resultados.FirstOrDefault(o => o.Id == id);
        WeakReferenceMessenger.Default.Send(new OrdemServicoAlteradaMessage());
    }

    [RelayCommand]
    private async Task HistoricoDaEmpresaAsync()
    {
        if (Selecionada is null)
            return;

        EmpresaFiltro = Selecionada.Empresa;
        TermoBusca = null;
        await BuscarAsync();
        MensagemStatus = $"Histórico de {EmpresaFiltro}: {Resultados.Count} auditoria(s).";
    }

    [RelayCommand]
    private async Task ExportarPdfAsync()
    {
        if (Resultados.Count == 0)
            return;

        var destino = await _fileDialog.SalvarComoAsync("relatorio-ordens-servico", "Documento PDF", "pdf");
        if (destino is null)
            return;

        await _pdfExport.ExportarRelatorioAsync(MontarTituloRelatorio(), Resultados.ToList(), destino);
        MensagemStatus = "Relatório PDF exportado.";
    }

    [RelayCommand]
    private async Task ExportarExcelAsync()
    {
        if (Resultados.Count == 0)
            return;

        var destino = await _fileDialog.SalvarComoAsync("ordens-servico", "Planilha do Excel", "xlsx");
        if (destino is null)
            return;

        await _excelExport.ExportarAsync(MontarTituloRelatorio(), Resultados.ToList(), destino);
        MensagemStatus = "Planilha Excel exportada.";
    }

    [RelayCommand]
    private void Voltar() => _navigation.IrParaInicio();

    private async Task InicializarAsync()
    {
        // Os resultados precisam aparecer mesmo que empresas/tags/logs falhem ao carregar
        // (ex.: banco ainda inicializando) — por isso rodam à parte, nunca bloqueando a busca.
        try
        {
            var empresas = await _ordemServicoService.ListarEmpresasAsync();
            Empresas.Clear();
            foreach (var empresa in empresas)
                Empresas.Add(empresa);

            var logs = await _ordemServicoService.ListarLogsAsync(30);
            Logs.Clear();
            foreach (var log in logs)
                Logs.Add(log);
        }
        catch (Exception excecao)
        {
            MensagemStatus = $"Falha ao carregar filtros: {excecao.Message}";
        }

        await BuscarAsync();
    }

    private void AtualizarIndicadores(IReadOnlyList<OrdemServico> resultados)
    {
        Total = resultados.Count;
        TotalAgendadas = resultados.Count(o => o.Situacao == SituacaoOS.Agendada);
        TotalEmAndamento = resultados.Count(o => o.Situacao == SituacaoOS.EmAndamento);
        TotalConcluidas = resultados.Count(o => o.Situacao == SituacaoOS.Concluida);
        TotalFavoritas = resultados.Count(o => o.Favorito);

        var maximoSituacao = Math.Max(1, resultados
            .GroupBy(o => o.Situacao)
            .Select(g => g.Count())
            .DefaultIfEmpty(0)
            .Max());

        DistribuicaoSituacao.Clear();
        foreach (var situacao in Enum.GetValues<SituacaoOS>())
        {
            var quantidade = resultados.Count(o => o.Situacao == situacao);
            if (quantidade == 0)
                continue;

            DistribuicaoSituacao.Add(new BarraDashboardViewModel(
                situacao.Descricao(), quantidade, quantidade / (double)maximoSituacao, situacao.Cor()));
        }

        var porTipo = resultados
            .GroupBy(o => o.Fiscalizacao.Descricao())
            .Select(g => (Nome: g.Key, Quantidade: g.Count()))
            .OrderByDescending(x => x.Quantidade)
            .Take(6)
            .ToList();

        var maximoTipo = Math.Max(1, porTipo.Select(x => x.Quantidade).DefaultIfEmpty(0).Max());

        DistribuicaoTipo.Clear();
        foreach (var (nome, quantidade) in porTipo)
            DistribuicaoTipo.Add(new BarraDashboardViewModel(
                nome, quantidade, quantidade / (double)maximoTipo, "#6366F1"));
    }

    private string MontarTituloRelatorio()
    {
        if (!string.IsNullOrWhiteSpace(EmpresaFiltro))
            return $"Histórico — {EmpresaFiltro}";

        if (SituacaoFiltro.HasValue)
            return $"Ordens de serviço — {SituacaoFiltro.Value.Descricao()}";

        return SomenteFavoritos ? "Ordens de serviço favoritas" : "Relatório de ordens de serviço";
    }

    private void ReiniciarDebounce()
    {
        _debounceBusca.Stop();
        _debounceBusca.Start();
    }

    public void Dispose()
    {
        _debounceBusca.Stop();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}

public sealed class BarraDashboardViewModel(string rotulo, int quantidade, double proporcao, string cor)
{
    public string Rotulo { get; } = rotulo;
    public int Quantidade { get; } = quantidade;
    public double Proporcao { get; } = proporcao;
    public string Cor { get; } = cor;

    /// <summary>Largura em pixels dentro da faixa fixa de 180px reservada ao gráfico.</summary>
    public double Largura { get; } = Math.Max(4, proporcao * 180);
}
