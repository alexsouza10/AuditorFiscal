using System.Collections.ObjectModel;
using System.Globalization;
using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Application.OrdensServico;
using AuditorFiscal.Application.OrdensServico.Dtos;
using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;
using AuditorFiscal.UI.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuditorFiscal.UI.ViewModels;

public partial class OrdemServicoFormViewModel : ViewModelBase, IDisposable
{
    private readonly OrdemServicoService _ordemServicoService;
    private readonly INavigationService _navigation;
    private readonly IFilePickerService _filePicker;
    private readonly IFileDialogService _fileDialog;
    private readonly IDialogService _dialogs;
    private readonly IPdfExportService _pdfExport;
    private readonly IPrintService _printService;
    private readonly DispatcherTimer _autoSaveTimer;

    private readonly List<NovoArquivoDto> _arquivosPendentes = [];
    private Guid _ordemServicoId;
    private bool _carregando;
    private bool _sujo;
    private bool _salvando;

    [ObservableProperty] private bool _isNovo = true;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private string _empresa = string.Empty;
    [ObservableProperty] private string _cnpj = string.Empty;
    [ObservableProperty] private string _endereco = string.Empty;
    [ObservableProperty] private string _cidade = string.Empty;
    [ObservableProperty] private string _responsavel = string.Empty;
    [ObservableProperty] private DateTimeOffset? _data = DateTimeOffset.Now;
    [ObservableProperty] private TimeSpan? _hora = new(9, 0, 0);
    [ObservableProperty] private string? _observacoes;
    [ObservableProperty] private string? _latitudeTexto;
    [ObservableProperty] private string? _longitudeTexto;
    [ObservableProperty] private TipoAuditoria? _tipoAuditoriaSelecionado;
    [ObservableProperty] private SituacaoOS _situacaoSelecionada = SituacaoOS.Agendada;
    [ObservableProperty] private bool _favorito;
    [ObservableProperty] private string? _mensagemErro;
    [ObservableProperty] private string? _mensagemStatus;

    public ObservableCollection<TipoAuditoria> TiposAuditoria { get; } = [];
    public ObservableCollection<ArquivoItemViewModel> Fotos { get; } = [];
    public ObservableCollection<ArquivoItemViewModel> Anexos { get; } = [];
    public ObservableCollection<TagSelecionavelViewModel> Tags { get; } = [];
    public ObservableCollection<TimelineEvento> Timeline { get; } = [];
    public IReadOnlyList<SituacaoOS> SituacoesDisponiveis { get; } = Enum.GetValues<SituacaoOS>();

    public OrdemServicoFormViewModel(
        OrdemServicoService ordemServicoService,
        INavigationService navigation,
        IFilePickerService filePicker,
        IFileDialogService fileDialog,
        IDialogService dialogs,
        IPdfExportService pdfExport,
        IPrintService printService)
    {
        _ordemServicoService = ordemServicoService;
        _navigation = navigation;
        _filePicker = filePicker;
        _fileDialog = fileDialog;
        _dialogs = dialogs;
        _pdfExport = pdfExport;
        _printService = printService;

        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _autoSaveTimer.Tick += async (_, _) => await SalvarAutomaticamenteAsync();
        _autoSaveTimer.Start();

        _ = InicializarAsync();
    }

    public string TituloPagina => IsNovo ? "Nova Ordem de Serviço" : $"Ordem de Serviço {Numero}";
    public bool PodeExportar => !IsNovo;

    public void DefinirAgendamentoInicial(DateOnly data, TimeOnly hora)
    {
        Data = data.ToDateTime(TimeOnly.MinValue);
        Hora = hora.ToTimeSpan();
    }

    public async Task CarregarParaEdicaoAsync(Guid ordemServicoId)
    {
        var ordemServico = await _ordemServicoService.ObterDetalheAsync(ordemServicoId);
        if (ordemServico is null)
            return;

        _carregando = true;
        try
        {
            _ordemServicoId = ordemServico.Id;
            IsNovo = false;
            Numero = ordemServico.Numero;
            Empresa = ordemServico.Empresa;
            Cnpj = ordemServico.Cnpj.Formatado();
            Endereco = ordemServico.Endereco;
            Cidade = ordemServico.Cidade;
            Responsavel = ordemServico.Responsavel;
            Data = ordemServico.Data.ToDateTime(TimeOnly.MinValue);
            Hora = ordemServico.Hora.ToTimeSpan();
            Observacoes = ordemServico.Observacoes;
            LatitudeTexto = ordemServico.Coordenada?.Latitude.ToString("F6", CultureInfo.InvariantCulture);
            LongitudeTexto = ordemServico.Coordenada?.Longitude.ToString("F6", CultureInfo.InvariantCulture);
            SituacaoSelecionada = ordemServico.Situacao;
            Favorito = ordemServico.Favorito;

            await CarregarTiposAuditoriaAsync();
            TipoAuditoriaSelecionado = TiposAuditoria.FirstOrDefault(t => t.Id == ordemServico.TipoAuditoriaId);

            AtualizarListasArquivos(ordemServico);

            Timeline.Clear();
            foreach (var evento in ordemServico.Timeline.OrderByDescending(t => t.OcorridoEm))
                Timeline.Add(evento);

            await CarregarTagsAsync(ordemServico.Tags.Select(t => t.Id).ToHashSet());
        }
        finally
        {
            _carregando = false;
            _sujo = false;
            NotificarCabecalho();
        }
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        MensagemErro = null;

        if (!TryConverterCoordenadas(out var latitude, out var longitude))
        {
            MensagemErro = "Latitude/Longitude inválidas.";
            return;
        }

        var arquivos = _arquivosPendentes.ToList();

        if (IsNovo)
        {
            var dto = new CriarOrdemServicoDto(
                Numero, Empresa, Cnpj, Endereco, Cidade, Responsavel,
                DateOnly.FromDateTime((Data ?? DateTimeOffset.Now).Date),
                TimeOnly.FromTimeSpan(Hora ?? TimeSpan.Zero),
                TipoAuditoriaSelecionado?.Id ?? Guid.Empty,
                Observacoes, latitude, longitude);

            var resultado = await _ordemServicoService.CriarAsync(dto, arquivos);
            if (!resultado.IsSuccess)
            {
                MensagemErro = resultado.Error;
                return;
            }

            _ordemServicoId = resultado.Value;
            _arquivosPendentes.Clear();
            IsNovo = false;

            if (SituacaoSelecionada != SituacaoOS.Agendada)
                await _ordemServicoService.AlterarSituacaoAsync(_ordemServicoId, SituacaoSelecionada);
        }
        else
        {
            var dto = new AtualizarOrdemServicoDto(
                _ordemServicoId, Empresa, Cnpj, Endereco, Cidade, Responsavel,
                DateOnly.FromDateTime((Data ?? DateTimeOffset.Now).Date),
                TimeOnly.FromTimeSpan(Hora ?? TimeSpan.Zero),
                TipoAuditoriaSelecionado?.Id ?? Guid.Empty,
                Observacoes, latitude, longitude);

            var resultado = await _ordemServicoService.AtualizarAsync(dto, arquivos);
            if (!resultado.IsSuccess)
            {
                MensagemErro = resultado.Error;
                return;
            }

            _arquivosPendentes.Clear();
            await _ordemServicoService.AlterarSituacaoAsync(_ordemServicoId, SituacaoSelecionada);
        }

        await _ordemServicoService.DefinirTagsAsync(
            _ordemServicoId, Tags.Where(t => t.Selecionada).Select(t => t.Id).ToList());

        _sujo = false;
        MensagemStatus = $"Salvo às {DateTime.Now:HH:mm:ss}";
        await CarregarParaEdicaoAsync(_ordemServicoId);
    }

    [RelayCommand]
    private async Task AdicionarFotosAsync()
    {
        foreach (var arquivo in await _filePicker.SelecionarImagensAsync())
            AdicionarPendente(new NovoArquivoDto(arquivo.NomeArquivo, arquivo.ContentType, arquivo.Conteudo, TipoArquivo.Foto));
    }

    [RelayCommand]
    private async Task AdicionarAnexosAsync()
    {
        foreach (var arquivo in await _filePicker.SelecionarArquivosAsync())
            AdicionarPendente(new NovoArquivoDto(arquivo.NomeArquivo, arquivo.ContentType, arquivo.Conteudo, TipoArquivo.Anexo));
    }

    [RelayCommand]
    private async Task RemoverArquivoAsync(ArquivoItemViewModel item)
    {
        if (item.EhPendente)
        {
            _arquivosPendentes.Remove(item.Pendente!);
            (item.Tipo == TipoArquivo.Foto ? Fotos : Anexos).Remove(item);
            _sujo = true;
            return;
        }

        if (!await _dialogs.ConfirmarAsync("Remover arquivo",
                $"Remover \"{item.NomeArquivo}\" permanentemente?", "Remover"))
            return;

        await _ordemServicoService.RemoverArquivoAsync(_ordemServicoId, item.IdPersistido!.Value);
        await CarregarParaEdicaoAsync(_ordemServicoId);
    }

    [RelayCommand]
    private async Task AlternarFavoritoAsync()
    {
        Favorito = !Favorito;

        if (!IsNovo)
            await _ordemServicoService.AlternarFavoritoAsync(_ordemServicoId);
    }

    [RelayCommand]
    private async Task ExcluirAsync()
    {
        if (IsNovo)
        {
            _navigation.Voltar();
            return;
        }

        if (!await _dialogs.ConfirmarAsync("Excluir ordem de serviço",
                $"Excluir a OS {Numero} e todos os seus anexos? Esta ação não pode ser desfeita.", "Excluir"))
            return;

        await _ordemServicoService.ExcluirAsync(_ordemServicoId);
        _navigation.Voltar();
    }

    [RelayCommand]
    private async Task ExportarPdfAsync()
    {
        var ordemServico = await _ordemServicoService.ObterDetalheAsync(_ordemServicoId);
        if (ordemServico is null)
            return;

        var destino = await _fileDialog.SalvarComoAsync($"OS-{Numero}", "Documento PDF", "pdf");
        if (destino is null)
            return;

        await _pdfExport.ExportarOrdemServicoAsync(ordemServico, destino);
        MensagemStatus = "PDF exportado.";
    }

    [RelayCommand]
    private async Task ImprimirAsync()
    {
        var ordemServico = await _ordemServicoService.ObterDetalheAsync(_ordemServicoId);
        if (ordemServico is null)
            return;

        try
        {
            await _printService.ImprimirAsync(ordemServico);
            MensagemStatus = "Enviado para a impressora.";
        }
        catch (Exception excecao)
        {
            MensagemErro = $"Não foi possível imprimir: {excecao.Message}";
        }
    }

    [RelayCommand]
    private void Voltar() => _navigation.Voltar();

    private void AdicionarPendente(NovoArquivoDto arquivo)
    {
        _arquivosPendentes.Add(arquivo);
        var item = ArquivoItemViewModel.DePendente(arquivo);
        (arquivo.Tipo == TipoArquivo.Foto ? Fotos : Anexos).Add(item);
        _sujo = true;
    }

    private void AtualizarListasArquivos(OrdemServico ordemServico)
    {
        Fotos.Clear();
        foreach (var foto in ordemServico.Fotos)
            Fotos.Add(ArquivoItemViewModel.DePersistido(foto.Id, foto.NomeOriginal, foto.TamanhoBytes, TipoArquivo.Foto));

        Anexos.Clear();
        foreach (var anexo in ordemServico.Anexos)
            Anexos.Add(ArquivoItemViewModel.DePersistido(anexo.Id, anexo.NomeOriginal, anexo.TamanhoBytes, TipoArquivo.Anexo));

        foreach (var pendente in _arquivosPendentes)
            (pendente.Tipo == TipoArquivo.Foto ? Fotos : Anexos).Add(ArquivoItemViewModel.DePendente(pendente));
    }

    private async Task InicializarAsync()
    {
        _carregando = true;
        try
        {
            await CarregarTiposAuditoriaAsync();
            await CarregarTagsAsync(new HashSet<Guid>());

            if (IsNovo && string.IsNullOrWhiteSpace(Numero))
                Numero = await _ordemServicoService.SugerirProximoNumeroAsync();
        }
        finally
        {
            _carregando = false;
            _sujo = false;
            NotificarCabecalho();
        }
    }

    private async Task CarregarTiposAuditoriaAsync()
    {
        var tipos = await _ordemServicoService.ListarTiposAuditoriaAtivosAsync();
        TiposAuditoria.Clear();
        foreach (var tipo in tipos)
            TiposAuditoria.Add(tipo);

        TipoAuditoriaSelecionado ??= TiposAuditoria.FirstOrDefault();
    }

    private async Task CarregarTagsAsync(IReadOnlySet<Guid> selecionadas)
    {
        var tags = await _ordemServicoService.ListarTagsAsync();
        Tags.Clear();
        foreach (var tag in tags)
            Tags.Add(new TagSelecionavelViewModel(tag.Id, tag.Nome, tag.Cor, selecionadas.Contains(tag.Id)));
    }

    private async Task SalvarAutomaticamenteAsync()
    {
        if (_salvando || !_sujo || IsNovo || string.IsNullOrWhiteSpace(Empresa))
            return;

        _salvando = true;
        try
        {
            await SalvarAsync();
        }
        finally
        {
            _salvando = false;
        }
    }

    private bool TryConverterCoordenadas(out double? latitude, out double? longitude)
    {
        latitude = null;
        longitude = null;

        if (!string.IsNullOrWhiteSpace(LatitudeTexto))
        {
            if (!double.TryParse(LatitudeTexto.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                return false;
            latitude = lat;
        }

        if (!string.IsNullOrWhiteSpace(LongitudeTexto))
        {
            if (!double.TryParse(LongitudeTexto.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                return false;
            longitude = lon;
        }

        return true;
    }

    private void NotificarCabecalho()
    {
        OnPropertyChanged(nameof(TituloPagina));
        OnPropertyChanged(nameof(PodeExportar));
    }

    protected override void OnPropertyChanged(global::System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (_carregando || e.PropertyName is nameof(MensagemErro) or nameof(MensagemStatus)
            or nameof(TituloPagina) or nameof(PodeExportar))
            return;

        if (e.PropertyName is nameof(IsNovo) or nameof(Numero))
            NotificarCabecalho();

        _sujo = true;
    }

    public void Dispose() => _autoSaveTimer.Stop();
}
