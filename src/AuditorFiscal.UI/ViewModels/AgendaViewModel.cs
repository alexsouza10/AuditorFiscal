using System.Collections.ObjectModel;
using System.Globalization;
using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Application.OrdensServico;
using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;
using AuditorFiscal.UI.Services;
using AuditorFiscal.UI.ViewModels.Agenda;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuditorFiscal.UI.ViewModels;

public partial class AgendaViewModel : ViewModelBase
{
    private static readonly CultureInfo CulturaPtBr = new("pt-BR");
    private const int PrimeiraHoraPadrao = 7;
    private const int UltimaHoraPadrao = 19;

    private readonly OrdemServicoService _ordemServicoService;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly IPdfExportService _pdfExport;
    private readonly IPrintService _printService;
    private readonly IFileDialogService _fileDialog;

    private DateOnly _inicioSemana;

    [ObservableProperty] private string _tituloPeriodo = string.Empty;
    [ObservableProperty] private EventoAgendaViewModel? _eventoSelecionado;
    [ObservableProperty] private SituacaoOS _situacaoEdicao;
    [ObservableProperty] private string? _mensagemStatus;

    public ObservableCollection<CabecalhoDiaViewModel> Cabecalhos { get; } = [];
    public ObservableCollection<LinhaHorarioViewModel> Linhas { get; } = [];
    public IReadOnlyList<SituacaoOS> SituacoesDisponiveis { get; } = Enum.GetValues<SituacaoOS>();

    public bool TemSelecao => EventoSelecionado is not null;

    public AgendaViewModel(
        OrdemServicoService ordemServicoService,
        INavigationService navigation,
        IDialogService dialogs,
        IPdfExportService pdfExport,
        IPrintService printService,
        IFileDialogService fileDialog)
    {
        _ordemServicoService = ordemServicoService;
        _navigation = navigation;
        _dialogs = dialogs;
        _pdfExport = pdfExport;
        _printService = printService;
        _fileDialog = fileDialog;

        _inicioSemana = InicioDaSemanaDe(DateOnly.FromDateTime(DateTime.Today));
        _ = CarregarAsync();
    }

    [RelayCommand]
    private async Task SemanaAnteriorAsync()
    {
        _inicioSemana = _inicioSemana.AddDays(-7);
        await CarregarAsync();
    }

    [RelayCommand]
    private async Task ProximaSemanaAsync()
    {
        _inicioSemana = _inicioSemana.AddDays(7);
        await CarregarAsync();
    }

    [RelayCommand]
    private async Task HojeAsync()
    {
        _inicioSemana = InicioDaSemanaDe(DateOnly.FromDateTime(DateTime.Today));
        await CarregarAsync();
    }

    [RelayCommand]
    private void SelecionarEvento(EventoAgendaViewModel evento)
    {
        if (EventoSelecionado is not null)
            EventoSelecionado.Selecionado = false;

        EventoSelecionado = evento;
        evento.Selecionado = true;
        SituacaoEdicao = evento.OrdemServico.Situacao;

        OnPropertyChanged(nameof(TemSelecao));
    }

    /// <summary>Clicar num horário vazio já abre o formulário com data e hora preenchidas.</summary>
    [RelayCommand]
    private void NovaOrdemNoHorario(CelulaAgendaViewModel celula)
    {
        var formulario = _navigation.Resolver<OrdemServicoFormViewModel>();
        formulario.DefinirAgendamentoInicial(celula.Data, celula.Hora);
        _navigation.NavegarPara(formulario);
    }

    [RelayCommand]
    private void NovaOrdem()
    {
        var formulario = _navigation.Resolver<OrdemServicoFormViewModel>();
        formulario.DefinirAgendamentoInicial(_inicioSemana, new TimeOnly(9, 0));
        _navigation.NavegarPara(formulario);
    }

    [RelayCommand]
    private async Task AbrirDetalheAsync()
    {
        if (EventoSelecionado is null)
            return;

        var formulario = _navigation.Resolver<OrdemServicoFormViewModel>();
        await formulario.CarregarParaEdicaoAsync(EventoSelecionado.Id);
        _navigation.NavegarPara(formulario);
    }

    [RelayCommand]
    private async Task AplicarSituacaoAsync()
    {
        if (EventoSelecionado is null)
            return;

        var resultado = await _ordemServicoService.AlterarSituacaoAsync(EventoSelecionado.Id, SituacaoEdicao);
        MensagemStatus = resultado.IsSuccess ? "Situação atualizada." : resultado.Error;
        await CarregarAsync(EventoSelecionado.Id);
    }

    [RelayCommand]
    private async Task AlternarFavoritoAsync()
    {
        if (EventoSelecionado is null)
            return;

        await _ordemServicoService.AlternarFavoritoAsync(EventoSelecionado.Id);
        await CarregarAsync(EventoSelecionado.Id);
    }

    [RelayCommand]
    private async Task ExcluirAsync()
    {
        if (EventoSelecionado is null)
            return;

        if (!await _dialogs.ConfirmarAsync("Excluir ordem de serviço",
                $"Excluir a OS {EventoSelecionado.Numero} ({EventoSelecionado.Empresa}) e seus anexos?", "Excluir"))
            return;

        await _ordemServicoService.ExcluirAsync(EventoSelecionado.Id);
        MensagemStatus = "Ordem de serviço excluída.";
        await CarregarAsync();
    }

    [RelayCommand]
    private async Task AdiarUmDiaAsync()
    {
        if (EventoSelecionado is null)
            return;

        var ordemServico = EventoSelecionado.OrdemServico;
        await _ordemServicoService.ReagendarAsync(ordemServico.Id, ordemServico.Data.AddDays(1), ordemServico.Hora);
        MensagemStatus = "Reagendada para o dia seguinte.";
        await CarregarAsync(ordemServico.Id);
    }

    [RelayCommand]
    private async Task ExportarPdfAsync()
    {
        if (EventoSelecionado is null)
            return;

        var ordemServico = await _ordemServicoService.ObterDetalheAsync(EventoSelecionado.Id);
        if (ordemServico is null)
            return;

        var destino = await _fileDialog.SalvarComoAsync($"OS-{ordemServico.Numero}", "Documento PDF", "pdf");
        if (destino is null)
            return;

        await _pdfExport.ExportarOrdemServicoAsync(ordemServico, destino);
        MensagemStatus = "PDF exportado.";
    }

    [RelayCommand]
    private async Task ImprimirAsync()
    {
        if (EventoSelecionado is null)
            return;

        var ordemServico = await _ordemServicoService.ObterDetalheAsync(EventoSelecionado.Id);
        if (ordemServico is null)
            return;

        try
        {
            await _printService.ImprimirAsync(ordemServico);
            MensagemStatus = "Enviado para a impressora.";
        }
        catch (Exception excecao)
        {
            MensagemStatus = $"Falha ao imprimir: {excecao.Message}";
        }
    }

    [RelayCommand]
    private void Voltar() => _navigation.IrParaInicio();

    private async Task CarregarAsync(Guid? manterSelecionado = null)
    {
        var fimSemana = _inicioSemana.AddDays(6);
        var ordens = await _ordemServicoService.ObterPorPeriodoAsync(_inicioSemana, fimSemana);

        TituloPeriodo = MontarTitulo(_inicioSemana, fimSemana);

        Cabecalhos.Clear();
        for (var i = 0; i < 7; i++)
            Cabecalhos.Add(new CabecalhoDiaViewModel(_inicioSemana.AddDays(i)));

        // A faixa de horas se ajusta para nunca esconder um compromisso fora do horário comercial.
        var primeiraHora = PrimeiraHoraPadrao;
        var ultimaHora = UltimaHoraPadrao;
        if (ordens.Count > 0)
        {
            primeiraHora = Math.Min(primeiraHora, ordens.Min(o => o.Hora.Hour));
            ultimaHora = Math.Max(ultimaHora, ordens.Max(o => o.Hora.Hour));
        }

        var porDiaHora = ordens
            .GroupBy(o => (o.Data, o.Hora.Hour))
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.Hora).ToList());

        Linhas.Clear();
        EventoAgendaViewModel? selecionar = null;

        for (var hora = primeiraHora; hora <= ultimaHora; hora++)
        {
            var celulas = new List<CelulaAgendaViewModel>(7);

            for (var dia = 0; dia < 7; dia++)
            {
                var data = _inicioSemana.AddDays(dia);
                var celula = new CelulaAgendaViewModel(data, new TimeOnly(hora, 0));

                if (porDiaHora.TryGetValue((data, hora), out var ordensDaCelula))
                {
                    foreach (var ordem in ordensDaCelula)
                    {
                        var evento = new EventoAgendaViewModel(ordem);
                        celula.Eventos.Add(evento);

                        if (manterSelecionado == ordem.Id)
                            selecionar = evento;
                    }
                }

                celulas.Add(celula);
            }

            Linhas.Add(new LinhaHorarioViewModel(new TimeOnly(hora, 0), celulas));
        }

        EventoSelecionado = null;
        OnPropertyChanged(nameof(TemSelecao));

        if (selecionar is not null)
            SelecionarEvento(selecionar);
    }

    private static string MontarTitulo(DateOnly inicio, DateOnly fim)
    {
        var mesInicio = inicio.ToDateTime(TimeOnly.MinValue).ToString("MMM", CulturaPtBr);
        var mesFim = fim.ToDateTime(TimeOnly.MinValue).ToString("MMM", CulturaPtBr);

        return inicio.Month == fim.Month
            ? $"{inicio.Day} – {fim.Day} de {Capitalizar(mesFim)} de {fim.Year}"
            : $"{inicio.Day} {Capitalizar(mesInicio)} – {fim.Day} {Capitalizar(mesFim)} {fim.Year}";
    }

    private static string Capitalizar(string texto) =>
        string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0], CulturaPtBr) + texto[1..].TrimEnd('.');

    private static DateOnly InicioDaSemanaDe(DateOnly data) =>
        data.AddDays(-(int)data.DayOfWeek);
}
