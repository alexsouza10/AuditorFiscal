using System.Collections.ObjectModel;
using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Application.OrdensServico;
using AuditorFiscal.Application.OrdensServico.Busca;
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
    [ObservableProperty] private string? _empresaFiltro;
    [ObservableProperty] private string? _cidadeFiltro;
    [ObservableProperty] private string? _responsavelFiltro;
    [ObservableProperty] private TipoFiscalizacao? _fiscalizacaoFiltro;
    [ObservableProperty] private DateTime? _dataInicioFiltro;
    [ObservableProperty] private DateTime? _dataFimFiltro;
    [ObservableProperty] private bool _somenteFavoritos;
    [ObservableProperty] private bool _somenteAtrasadas;
    [ObservableProperty] private OrdemServico? _selecionada;
    [ObservableProperty] private OrdemServicoLinhaViewModel? _linhaSelecionada;
    [ObservableProperty] private string? _mensagemStatus;
    [ObservableProperty] private int _total;
    [ObservableProperty] private int _totalAgendadas;
    [ObservableProperty] private int _totalEmAndamento;
    [ObservableProperty] private int _totalConcluidas;
    [ObservableProperty] private int _totalFavoritas;
    [ObservableProperty] private int _paginaAtual = 1;
    [ObservableProperty] private int _tamanhoPagina = 50;
    [ObservableProperty] private bool _selecionarTodos;

    /// <summary>Lista completa filtrada (todas as páginas); <see cref="Resultados"/> é apenas a janela exibida.</summary>
    private List<OrdemServicoLinhaViewModel> _todosItens = [];

    public ObservableCollection<OrdemServicoLinhaViewModel> Resultados { get; } = [];
    public ObservableCollection<string> Empresas { get; } = [];
    public ObservableCollection<BarraDashboardViewModel> DistribuicaoSituacao { get; } = [];
    public ObservableCollection<BarraDashboardViewModel> DistribuicaoTipo { get; } = [];
    public ObservableCollection<LogInterno> Logs { get; } = [];
    public ObservableCollection<TimelineEvento> TimelineSelecionada { get; } = [];

    public SituacaoMultiSelectViewModel SituacaoFiltro { get; } = new();

    public IReadOnlyList<TipoFiscalizacao?> FiscalizacoesFiltro { get; } =
        new TipoFiscalizacao?[] { null }.Concat(Enum.GetValues<TipoFiscalizacao>().Cast<TipoFiscalizacao?>()).ToList();

    public IReadOnlyList<int> TamanhosPagina { get; } = [20, 50, 100, 200];

    public bool TemSelecao => Selecionada is not null;

    public int TotalPaginas => _todosItens.Count == 0
        ? 1
        : (int)Math.Ceiling(_todosItens.Count / (double)TamanhoPagina);

    public int TotalSelecionados => _todosItens.Count(i => i.Selecionada);

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

        SituacaoFiltro.SelecaoAlterada += (_, _) => ReiniciarDebounce();

        _ = InicializarAsync();
    }

    partial void OnTermoBuscaChanged(string? value) => ReiniciarDebounce();
    partial void OnEmpresaFiltroChanged(string? value) => ReiniciarDebounce();
    partial void OnCidadeFiltroChanged(string? value) => ReiniciarDebounce();
    partial void OnResponsavelFiltroChanged(string? value) => ReiniciarDebounce();
    partial void OnFiscalizacaoFiltroChanged(TipoFiscalizacao? value) => ReiniciarDebounce();
    partial void OnDataInicioFiltroChanged(DateTime? value) => ReiniciarDebounce();
    partial void OnDataFimFiltroChanged(DateTime? value) => ReiniciarDebounce();
    partial void OnSomenteFavoritosChanged(bool value) => ReiniciarDebounce();
    partial void OnSomenteAtrasadasChanged(bool value) => ReiniciarDebounce();

    partial void OnSelecionadaChanged(OrdemServico? value)
    {
        OnPropertyChanged(nameof(TemSelecao));
        TimelineSelecionada.Clear();

        if (value is null)
            return;

        foreach (var evento in value.Timeline.OrderByDescending(t => t.OcorridoEm))
            TimelineSelecionada.Add(evento);
    }

    /// <summary>A grade seleciona uma linha (com checkbox de exportação); o painel de detalhe
    /// e os comandos de ação continuam trabalhando com a entidade pura.</summary>
    partial void OnLinhaSelecionadaChanged(OrdemServicoLinhaViewModel? value) => Selecionada = value?.OrdemServico;

    partial void OnTamanhoPaginaChanged(int value)
    {
        if (PaginaAtual != 1)
            PaginaAtual = 1;
        else
            AtualizarPagina();
    }

    partial void OnPaginaAtualChanged(int value) => AtualizarPagina();

    /// <summary>Aplica a todo o conjunto filtrado (não só à página exibida), para permitir
    /// selecionar e exportar mais O.S. do que cabem em uma página.</summary>
    partial void OnSelecionarTodosChanged(bool value)
    {
        foreach (var item in _todosItens)
            item.Selecionada = value;
    }

    [RelayCommand]
    private async Task BuscarAsync()
    {
        // A caixa de busca aceita a query DSL ("empresa:x prazo<5 atrasadas favoritas" etc.);
        // os controles dedicados da tela têm prioridade quando também preenchidos.
        var interpretado = ConsultaOrdemServicoParser.Interpretar(TermoBusca);
        var filtro = interpretado with
        {
            Situacao = SituacaoFiltro.Selecionadas.Count == 0 ? interpretado.Situacao : null,
            Situacoes = SituacaoFiltro.Selecionadas.Count > 0 ? SituacaoFiltro.Selecionadas : null,
            Fiscalizacao = FiscalizacaoFiltro ?? interpretado.Fiscalizacao,
            SomenteFavoritos = SomenteFavoritos || interpretado.SomenteFavoritos,
            SomenteAtrasadas = SomenteAtrasadas || interpretado.SomenteAtrasadas,
            CidadeContem = string.IsNullOrWhiteSpace(CidadeFiltro) ? interpretado.CidadeContem : CidadeFiltro,
            ResponsavelContem = string.IsNullOrWhiteSpace(ResponsavelFiltro) ? interpretado.ResponsavelContem : ResponsavelFiltro,
            DataInicio = DataInicioFiltro is not null ? DateOnly.FromDateTime(DataInicioFiltro.Value) : interpretado.DataInicio,
            DataFim = DataFimFiltro is not null ? DateOnly.FromDateTime(DataFimFiltro.Value) : interpretado.DataFim
        };

        var resultados = await _ordemServicoService.BuscarAsync(filtro);

        if (!string.IsNullOrWhiteSpace(EmpresaFiltro))
            resultados = resultados.Where(o => o.Empresa == EmpresaFiltro).ToList();

        _todosItens = resultados.Select(CriarLinha).ToList();
        SelecionarTodos = false;

        if (PaginaAtual != 1)
            PaginaAtual = 1;
        else
            AtualizarPagina();

        AtualizarIndicadores(resultados);
        MensagemStatus = $"{resultados.Count} resultado(s).";
    }

    [RelayCommand]
    private async Task LimparFiltrosAsync()
    {
        TermoBusca = null;
        SituacaoFiltro.TodasSituacoes = true;
        EmpresaFiltro = null;
        CidadeFiltro = null;
        ResponsavelFiltro = null;
        FiscalizacaoFiltro = null;
        DataInicioFiltro = null;
        DataFimFiltro = null;
        SomenteFavoritos = false;
        SomenteAtrasadas = false;
        await BuscarAsync();
    }

    [RelayCommand]
    private void PaginaAnterior()
    {
        if (PaginaAtual > 1)
            PaginaAtual--;
    }

    [RelayCommand]
    private void ProximaPagina()
    {
        if (PaginaAtual < TotalPaginas)
            PaginaAtual++;
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
        LinhaSelecionada = null;
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
        LinhaSelecionada = _todosItens.FirstOrDefault(i => i.OrdemServico.Id == id);
        WeakReferenceMessenger.Default.Send(new OrdemServicoAlteradaMessage());
    }

    [RelayCommand]
    private async Task HistoricoDaEmpresaAsync()
    {
        if (Selecionada is null)
            return;

        // Situação/favoritos ativos escondiam parte do histórico da empresa (a busca local
        // por empresa era aplicada por cima desses filtros). Limpamos tudo aqui para garantir
        // o histórico completo, e paramos o debounce para uma 2ª busca disparada pelas
        // mudanças acima não sobrescrever a mensagem de status logo em seguida.
        TermoBusca = null;
        SituacaoFiltro.TodasSituacoes = true;
        SomenteFavoritos = false;
        EmpresaFiltro = Selecionada.Empresa;

        _debounceBusca.Stop();
        await BuscarAsync();
        MensagemStatus = $"Histórico de {EmpresaFiltro}: {_todosItens.Count} auditoria(s).";
    }

    [RelayCommand]
    private async Task ExportarPdfAsync()
    {
        var itens = ItensParaExportar();
        if (itens.Count == 0)
            return;

        var destino = await _fileDialog.SalvarComoAsync("relatorio-ordens-servico", "Documento PDF", "pdf");
        if (destino is null)
            return;

        await _pdfExport.ExportarRelatorioAsync(MontarTituloRelatorio(), itens, destino);
        MensagemStatus = $"Relatório PDF exportado ({itens.Count} O.S.).";
    }

    [RelayCommand]
    private async Task ExportarExcelAsync()
    {
        var itens = ItensParaExportar();
        if (itens.Count == 0)
            return;

        var destino = await _fileDialog.SalvarComoAsync("ordens-servico", "Planilha do Excel", "xlsx");
        if (destino is null)
            return;

        await _excelExport.ExportarAsync(MontarTituloRelatorio(), itens, destino);
        MensagemStatus = $"Planilha Excel exportada ({itens.Count} O.S.).";
    }

    [RelayCommand]
    private void Voltar() => _navigation.IrParaInicio();

    [RelayCommand]
    private void FecharDetalhe() => LinhaSelecionada = null;

    /// <summary>Exporta apenas as O.S. marcadas com checkbox; sem nenhuma marcada, exporta
    /// todo o resultado filtrado (todas as páginas), não só a página exibida na grade.</summary>
    private List<OrdemServico> ItensParaExportar()
    {
        var selecionados = _todosItens.Where(i => i.Selecionada).Select(i => i.OrdemServico).ToList();
        return selecionados.Count > 0 ? selecionados : _todosItens.Select(i => i.OrdemServico).ToList();
    }

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

        var situacoes = SituacaoFiltro.Selecionadas;
        if (situacoes.Count > 0)
            return $"Ordens de serviço — {string.Join(", ", situacoes.Select(s => s.Descricao()))}";

        return SomenteFavoritos ? "Ordens de serviço favoritas" : "Relatório de ordens de serviço";
    }

    private void ReiniciarDebounce()
    {
        _debounceBusca.Stop();
        _debounceBusca.Start();
    }

    /// <summary>Recorta de <see cref="_todosItens"/> a fatia correspondente à página atual
    /// para dentro de <see cref="Resultados"/>, sem refazer a consulta ao banco.</summary>
    private void AtualizarPagina()
    {
        var totalPaginas = TotalPaginas;
        if (PaginaAtual > totalPaginas)
        {
            PaginaAtual = totalPaginas;
            return;
        }

        Resultados.Clear();
        foreach (var item in _todosItens.Skip((PaginaAtual - 1) * TamanhoPagina).Take(TamanhoPagina))
            Resultados.Add(item);

        OnPropertyChanged(nameof(TotalPaginas));
    }

    private OrdemServicoLinhaViewModel CriarLinha(OrdemServico ordemServico)
    {
        var linha = new OrdemServicoLinhaViewModel(ordemServico);
        linha.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(OrdemServicoLinhaViewModel.Selecionada))
                OnPropertyChanged(nameof(TotalSelecionados));
        };
        return linha;
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

/// <summary>Envolve uma O.S. com o estado de seleção (checkbox) usado pela grade do Banco de
/// Dados para escolher quais registros entram na exportação de PDF/Excel.</summary>
public sealed partial class OrdemServicoLinhaViewModel(OrdemServico ordemServico) : ObservableObject
{
    public OrdemServico OrdemServico { get; } = ordemServico;

    [ObservableProperty] private bool _selecionada;

    public string Numero => OrdemServico.Numero;
    public string Empresa => OrdemServico.Empresa;
    public string Cidade => OrdemServico.Cidade;
    public DateOnly RecebimentoSfit => OrdemServico.RecebimentoSfit;
    public DateOnly DataFinal => OrdemServico.DataFinal;
    public SituacaoOS Situacao => OrdemServico.Situacao;
}
