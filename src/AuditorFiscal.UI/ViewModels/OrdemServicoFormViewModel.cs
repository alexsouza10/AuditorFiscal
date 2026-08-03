using System.Collections.ObjectModel;
using System.Globalization;
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

public partial class OrdemServicoFormViewModel : ViewModelBase, IDisposable
{
    private readonly OrdemServicoService _ordemServicoService;
    private readonly INavigationService _navigation;
    private readonly IFilePickerService _filePicker;
    private readonly IFileDialogService _fileDialog;
    private readonly IDialogService _dialogs;
    private readonly IPdfExportService _pdfExport;
    private readonly ICepLookupService _cepLookup;
    private readonly DispatcherTimer _autoSaveTimer;

    private readonly List<NovoArquivoDto> _arquivosPendentes = [];
    private Guid _ordemServicoId;
    private bool _carregando;
    private bool _sujo;
    private bool _salvando;

    // Se o auditor deixar o campo "O.S. N°" em branco, cai em um desses dois: o número
    // sugerido (nova OS) ou o número que já estava salvo (edição) — nunca fica sem número.
    private string _numeroSugerido = string.Empty;
    private string _numeroCarregado = string.Empty;
    private SituacaoOS _situacaoCarregada = SituacaoOS.EmAndamento;

    // Base do Viewbox em 100% de zoom (mesmos valores usados antes de o zoom manual existir).
    private const double ZoomViewboxLarguraBase = 1560;
    private const double ZoomViewboxAlturaBase = 860;
    private const double ZoomMinimo = 0.5;
    private const double ZoomMaximo = 2.0;
    private const double ZoomPasso = 0.1;

    [ObservableProperty] private bool _isNovo = true;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private string? _numeroPlaceholder;
    [ObservableProperty] private string _empresa = string.Empty;
    [ObservableProperty] private string _cnpj = string.Empty;
    [ObservableProperty] private string? _cep;
    [ObservableProperty] private string _endereco = string.Empty;
    [ObservableProperty] private string _cidade = string.Empty;
    [ObservableProperty] private string _responsavel = string.Empty;
    [ObservableProperty] private bool _mostrarArquivos;
    [ObservableProperty] private string? _mensagemCep;
    [ObservableProperty] private TipoFiscalizacao _fiscalizacaoSelecionada = TipoFiscalizacao.Direta;

    // As etapas do fluxo SFIT (CLAUDE V2), na ordem em que a auditoria progride. Ficam em
    // branco por padrão — o auditor deve digitar ou escolher cada data conscientemente,
    // em vez de herdar um valor pré-preenchido que poderia passar despercebido.
    [ObservableProperty] private DateTime? _recebimentoSfit;
    [ObservableProperty] private DateTime? _aberturaSfit;
    [ObservableProperty] private DateTime? _dataFiscalizacao;
    [ObservableProperty] private DateTime? _prazoNad;
    [ObservableProperty] private DateTime? _prazoNco;
    [ObservableProperty] private DateTime? _elaboracaoAutos;
    [ObservableProperty] private DateTime? _dataFinal;

    [ObservableProperty] private string? _observacoes;
    [ObservableProperty] private string? _latitudeTexto;
    [ObservableProperty] private string? _longitudeTexto;
    [ObservableProperty] private SituacaoOS _situacaoSelecionada = SituacaoOS.EmAndamento;
    [ObservableProperty] private bool _favorito;
    [ObservableProperty] private bool _temNcre;
    [ObservableProperty] private DateTime? _prazoNcre;
    [ObservableProperty] private string? _mensagemErro;
    [ObservableProperty] private string? _mensagemStatus;
    [ObservableProperty] private int _abaSelecionada;
    [ObservableProperty] private double _zoomNivel = 1.0;

    public ObservableCollection<ArquivoItemViewModel> Fotos { get; } = [];
    public ObservableCollection<ArquivoItemViewModel> Anexos { get; } = [];
    public ObservableCollection<TimelineEvento> Timeline { get; } = [];
    public IReadOnlyList<SituacaoOS> SituacoesDisponiveis { get; } = Enum.GetValues<SituacaoOS>();
    public IReadOnlyList<TipoFiscalizacao> TiposFiscalizacaoDisponiveis { get; } = Enum.GetValues<TipoFiscalizacao>();

    public OrdemServicoFormViewModel(
        OrdemServicoService ordemServicoService,
        INavigationService navigation,
        IFilePickerService filePicker,
        IFileDialogService fileDialog,
        IDialogService dialogs,
        IPdfExportService pdfExport,
        ICepLookupService cepLookup)
    {
        _ordemServicoService = ordemServicoService;
        _navigation = navigation;
        _filePicker = filePicker;
        _fileDialog = fileDialog;
        _dialogs = dialogs;
        _pdfExport = pdfExport;
        _cepLookup = cepLookup;

        // Debounce (reinicia a cada edição, ver OnPropertyChanged) em vez de varredura
        // periódica: sem isso, uma alteração feita logo após um "tick" só era salva —
        // e só refletia no cronograma GANTT — até 5s depois, mesmo já estando ocioso.
        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _autoSaveTimer.Tick += async (_, _) =>
        {
            _autoSaveTimer.Stop();
            await SalvarAutomaticamenteAsync();
        };

        _ = InicializarAsync();
    }

    public string TituloPagina => IsNovo ? "Nova Ordem de Serviço" : $"Ordem de Serviço {Numero}";
    public bool PodeExportar => !IsNovo;
    public string FavoritoTexto => Favorito ? "★ Favorito" : "☆ Favorito";
    public double ZoomViewboxLargura => ZoomViewboxLarguraBase * ZoomNivel;
    public double ZoomViewboxAltura => ZoomViewboxAlturaBase * ZoomNivel;
    public string ZoomTexto => $"{ZoomNivel:P0}";

    partial void OnFavoritoChanged(bool value) => OnPropertyChanged(nameof(FavoritoTexto));

    partial void OnZoomNivelChanged(double value)
    {
        OnPropertyChanged(nameof(ZoomViewboxLargura));
        OnPropertyChanged(nameof(ZoomViewboxAltura));
        OnPropertyChanged(nameof(ZoomTexto));
        AumentarZoomCommand.NotifyCanExecuteChanged();
        DiminuirZoomCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(PodeAumentarZoom))]
    private void AumentarZoom() => ZoomNivel = Math.Round(Math.Min(ZoomMaximo, ZoomNivel + ZoomPasso), 2);

    private bool PodeAumentarZoom() => ZoomNivel < ZoomMaximo;

    [RelayCommand(CanExecute = nameof(PodeDiminuirZoom))]
    private void DiminuirZoom() => ZoomNivel = Math.Round(Math.Max(ZoomMinimo, ZoomNivel - ZoomPasso), 2);

    private bool PodeDiminuirZoom() => ZoomNivel > ZoomMinimo;

    public void DefinirAgendamentoInicial(DateOnly data, TimeOnly hora)
    {
        RecebimentoSfit = data.ToDateTime(hora);
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
            _numeroCarregado = ordemServico.Numero;
            NumeroPlaceholder = $"Em branco = mantém {ordemServico.Numero}";
            Empresa = ordemServico.Empresa;
            Cnpj = ordemServico.Cnpj.Formatado();
            Endereco = ordemServico.Endereco;
            Cidade = ordemServico.Cidade;
            Responsavel = ordemServico.Responsavel ?? string.Empty;
            FiscalizacaoSelecionada = ordemServico.Fiscalizacao;
            RecebimentoSfit = ordemServico.RecebimentoSfit.ToDateTime(TimeOnly.MinValue);
            AberturaSfit = ordemServico.AberturaSfit.ToDateTime(TimeOnly.MinValue);
            DataFiscalizacao = ordemServico.DataFiscalizacao.ToDateTime(TimeOnly.MinValue);
            PrazoNad = ordemServico.PrazoNad.ToDateTime(TimeOnly.MinValue);
            PrazoNco = ordemServico.PrazoNco.ToDateTime(TimeOnly.MinValue);
            ElaboracaoAutos = ordemServico.ElaboracaoAutos.ToDateTime(TimeOnly.MinValue);
            DataFinal = ordemServico.DataFinal.ToDateTime(TimeOnly.MinValue);
            Observacoes = ordemServico.Observacoes;
            LatitudeTexto = ordemServico.Coordenada?.Latitude.ToString("F6", CultureInfo.InvariantCulture);
            LongitudeTexto = ordemServico.Coordenada?.Longitude.ToString("F6", CultureInfo.InvariantCulture);
            SituacaoSelecionada = ordemServico.Situacao;
            _situacaoCarregada = ordemServico.Situacao;
            Favorito = ordemServico.Favorito;
            TemNcre = ordemServico.TemNcre;
            PrazoNcre = ordemServico.PrazoNcre?.ToDateTime(TimeOnly.MinValue);

            AtualizarListasArquivos(ordemServico);

            Timeline.Clear();
            foreach (var evento in ordemServico.Timeline.OrderByDescending(t => t.OcorridoEm))
                Timeline.Add(evento);
        }
        finally
        {
            _carregando = false;
            _sujo = false;
            NotificarCabecalho();
        }
    }

    [RelayCommand]
    private async Task SalvarAsync() => await SalvarInternoAsync();

    [RelayCommand]
    private async Task SalvarECriarNovaAsync()
    {
        if (await SalvarInternoAsync())
            await LimparParaNovaAsync();
    }

    private async Task<bool> SalvarInternoAsync()
    {
        MensagemErro = null;

        if (!DatasObrigatoriasPreenchidas())
        {
            MensagemErro = "Preencha todas as datas do fluxo SFIT antes de salvar.";
            return false;
        }

        if (!TryConverterCoordenadas(out var latitude, out var longitude))
        {
            MensagemErro = "Latitude/Longitude inválidas.";
            return false;
        }

        var arquivos = _arquivosPendentes.ToList();
        var numero = string.IsNullOrWhiteSpace(Numero)
            ? (IsNovo ? _numeroSugerido : _numeroCarregado)
            : Numero.Trim();

        if (IsNovo)
        {
            var dto = new CriarOrdemServicoDto(
                numero, Empresa, Cnpj, Endereco, Cidade, Responsavel, FiscalizacaoSelecionada,
                ParaData(RecebimentoSfit), ParaData(AberturaSfit), ParaData(DataFiscalizacao), ParaData(PrazoNad),
                ParaData(PrazoNco), ParaData(ElaboracaoAutos), ParaData(DataFinal),
                Observacoes, latitude, longitude, TemNcre, TemNcre ? ParaData(PrazoNcre) : null);

            var resultado = await _ordemServicoService.CriarAsync(dto, arquivos);
            if (!resultado.IsSuccess)
            {
                MensagemErro = resultado.Error;
                return false;
            }

            _ordemServicoId = resultado.Value;
            _arquivosPendentes.Clear();
            IsNovo = false;

            if (SituacaoSelecionada != SituacaoOS.EmAndamento)
                await _ordemServicoService.AlterarSituacaoAsync(_ordemServicoId, SituacaoSelecionada);
        }
        else
        {
            var dto = new AtualizarOrdemServicoDto(
                _ordemServicoId, numero, Empresa, Cnpj, Endereco, Cidade, Responsavel, FiscalizacaoSelecionada,
                ParaData(RecebimentoSfit), ParaData(AberturaSfit), ParaData(DataFiscalizacao), ParaData(PrazoNad),
                ParaData(PrazoNco), ParaData(ElaboracaoAutos), ParaData(DataFinal),
                Observacoes, latitude, longitude, TemNcre, TemNcre ? ParaData(PrazoNcre) : null);

            var resultado = await _ordemServicoService.AtualizarAsync(dto, arquivos);
            if (!resultado.IsSuccess)
            {
                MensagemErro = resultado.Error;
                return false;
            }

            _arquivosPendentes.Clear();

            // Só dispara a chamada (recarrega a entidade inteira e grava de novo) quando a
            // situação realmente mudou — antes isso rodava em todo Salvar, dobrando o custo
            // de cada gravação à toa.
            if (SituacaoSelecionada != _situacaoCarregada)
                await _ordemServicoService.AlterarSituacaoAsync(_ordemServicoId, SituacaoSelecionada);
        }

        _sujo = false;
        MensagemStatus = $"Salvo às {DateTime.Now:HH:mm:ss}";
        await CarregarParaEdicaoAsync(_ordemServicoId);
        WeakReferenceMessenger.Default.Send(new OrdemServicoAlteradaMessage());
        return true;
    }

    /// <summary>Reseta o formulário para um novo cadastro logo após salvar, poupando o auditor de
    /// voltar à tela inicial quando precisa lançar várias O.S. em sequência.</summary>
    private async Task LimparParaNovaAsync()
    {
        _carregando = true;
        try
        {
            _ordemServicoId = Guid.Empty;
            IsNovo = true;
            Numero = string.Empty;
            _numeroCarregado = string.Empty;
            Empresa = string.Empty;
            Cnpj = string.Empty;
            Cep = null;
            MensagemCep = null;
            Endereco = string.Empty;
            Cidade = string.Empty;
            Responsavel = string.Empty;
            FiscalizacaoSelecionada = TipoFiscalizacao.Direta;
            RecebimentoSfit = null;
            AberturaSfit = null;
            DataFiscalizacao = null;
            PrazoNad = null;
            PrazoNco = null;
            ElaboracaoAutos = null;
            DataFinal = null;
            Observacoes = null;
            LatitudeTexto = null;
            LongitudeTexto = null;
            SituacaoSelecionada = SituacaoOS.EmAndamento;
            Favorito = false;
            TemNcre = false;
            PrazoNcre = null;
            MostrarArquivos = false;

            _arquivosPendentes.Clear();
            Fotos.Clear();
            Anexos.Clear();
            Timeline.Clear();

            _numeroSugerido = await _ordemServicoService.SugerirProximoNumeroAsync();
            NumeroPlaceholder = $"Em branco = usa {_numeroSugerido}";
            MensagemStatus = "Ordem de serviço salva. Formulário pronto para uma nova.";
        }
        finally
        {
            _carregando = false;
            _sujo = false;
            NotificarCabecalho();
        }
    }

    [RelayCommand]
    private async Task BuscarCepAsync()
    {
        MensagemCep = null;

        if (string.IsNullOrWhiteSpace(Cep))
            return;

        var endereco = await _cepLookup.BuscarAsync(Cep);
        if (endereco is null)
        {
            MensagemCep = "CEP não encontrado ou sem conexão com a internet.";
            return;
        }

        Endereco = string.IsNullOrWhiteSpace(endereco.Bairro)
            ? endereco.Logradouro
            : $"{endereco.Logradouro}, {endereco.Bairro}";
        Cidade = string.IsNullOrWhiteSpace(endereco.Uf) ? endereco.Cidade : $"{endereco.Cidade}/{endereco.Uf}";
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
        {
            await _ordemServicoService.AlternarFavoritoAsync(_ordemServicoId);
            WeakReferenceMessenger.Default.Send(new OrdemServicoAlteradaMessage());
        }
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
        WeakReferenceMessenger.Default.Send(new OrdemServicoAlteradaMessage());
        _navigation.Voltar();
    }

    [RelayCommand]
    private async Task ExportarPdfAsync()
    {
        // Garante que anexos/alterações recém-adicionados (ainda não persistidos pelo
        // auto-save) estejam no banco antes de exportar — senão o PDF sai desatualizado.
        if (_sujo)
            await SalvarAsync();

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
    private async Task Voltar()
    {
        // Flush do autosave pendente: sem isso, sair da tela logo após uma edição podia
        // perder a alteração (timer é parado no Dispose) ou deixar o GANTT desatualizado
        // até a próxima vez que essa OS fosse salva.
        await SalvarAutomaticamenteAsync();
        _navigation.Voltar();
    }

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
            if (IsNovo)
            {
                // Fica em branco por padrão: o auditor escolhe um número próprio ou deixa o
                // campo vazio para usar a sugestão automática só na hora de salvar.
                _numeroSugerido = await _ordemServicoService.SugerirProximoNumeroAsync();
                NumeroPlaceholder = $"Em branco = usa {_numeroSugerido}";
            }
        }
        finally
        {
            _carregando = false;
            _sujo = false;
            NotificarCabecalho();
        }
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

    private bool DatasObrigatoriasPreenchidas()
    {
        if (RecebimentoSfit is null || AberturaSfit is null || DataFiscalizacao is null ||
            PrazoNad is null || PrazoNco is null || ElaboracaoAutos is null || DataFinal is null)
            return false;

        return !TemNcre || PrazoNcre is not null;
    }

    private static DateOnly ParaData(DateTime? valor) =>
        DateOnly.FromDateTime((valor ?? DateTime.Now).Date);

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
            or nameof(TituloPagina) or nameof(PodeExportar) or nameof(FavoritoTexto) or nameof(AbaSelecionada)
            or nameof(MensagemCep) or nameof(MostrarArquivos) or nameof(Cep) or nameof(NumeroPlaceholder)
            or nameof(ZoomNivel) or nameof(ZoomViewboxLargura) or nameof(ZoomViewboxAltura) or nameof(ZoomTexto))
            return;

        if (e.PropertyName is nameof(IsNovo) or nameof(Numero))
            NotificarCabecalho();

        _sujo = true;
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    public void Dispose() => _autoSaveTimer.Stop();
}
