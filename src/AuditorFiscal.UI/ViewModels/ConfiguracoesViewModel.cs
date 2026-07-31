using System.Collections.ObjectModel;
using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuditorFiscal.UI.ViewModels;

public partial class ConfiguracoesViewModel : ViewModelBase
{
    private readonly IPreferencesService _preferencias;
    private readonly IAutoStartService _autoStart;
    private readonly IBackupService _backup;
    private readonly IThemeService _tema;
    private readonly IDialogService _dialogs;
    private readonly IFileDialogService _fileDialog;
    private readonly INavigationService _navigation;

    private bool _carregando = true;

    [ObservableProperty] private TemaAplicacao _temaSelecionado;
    [ObservableProperty] private bool _iniciarComWindows;
    [ObservableProperty] private bool _backupAutomatico;
    [ObservableProperty] private string? _mensagemStatus;

    public ObservableCollection<string> BackupsDisponiveis { get; } = [];
    public IReadOnlyList<TemaAplicacao> TemasDisponiveis { get; } = Enum.GetValues<TemaAplicacao>();

    public ConfiguracoesViewModel(
        IPreferencesService preferencias,
        IAutoStartService autoStart,
        IBackupService backup,
        IThemeService tema,
        IDialogService dialogs,
        IFileDialogService fileDialog,
        INavigationService navigation)
    {
        _preferencias = preferencias;
        _autoStart = autoStart;
        _backup = backup;
        _tema = tema;
        _dialogs = dialogs;
        _fileDialog = fileDialog;
        _navigation = navigation;

        _temaSelecionado = preferencias.Atual.Tema;
        _backupAutomatico = preferencias.Atual.BackupAutomatico;
        _iniciarComWindows = autoStart.EstaHabilitado();
        _carregando = false;

        AtualizarListaBackups();
    }

    partial void OnTemaSelecionadoChanged(TemaAplicacao value)
    {
        if (_carregando)
            return;

        _tema.Aplicar(value);
    }

    partial void OnIniciarComWindowsChanged(bool value)
    {
        if (_carregando)
            return;

        if (value)
            _autoStart.Habilitar();
        else
            _autoStart.Desabilitar();

        _preferencias.Salvar(_preferencias.Atual with { IniciarComWindows = value });
        MensagemStatus = value
            ? "O aplicativo será iniciado junto com o Windows."
            : "Inicialização automática desativada.";
    }

    partial void OnBackupAutomaticoChanged(bool value)
    {
        if (_carregando)
            return;

        _preferencias.Salvar(_preferencias.Atual with { BackupAutomatico = value });
    }

    [RelayCommand]
    private async Task CriarBackupAsync()
    {
        try
        {
            var registro = await _backup.CriarBackupAsync(automatico: false);
            _preferencias.Salvar(_preferencias.Atual with { UltimoBackup = registro.CriadoEm });
            AtualizarListaBackups();
            MensagemStatus = $"Backup criado: {Path.GetFileName(registro.CaminhoArquivo)}";
        }
        catch (Exception excecao)
        {
            MensagemStatus = $"Falha ao criar backup: {excecao.Message}";
        }
    }

    [RelayCommand]
    private async Task RestaurarBackupAsync()
    {
        var caminho = await _fileDialog.AbrirArquivoAsync("Backup do Auditor Fiscal", "afbkp");
        if (caminho is null)
            return;

        if (!await _dialogs.ConfirmarAsync("Restaurar backup",
                "Todos os dados atuais serão substituídos pelo conteúdo do backup. " +
                "Um backup de segurança do estado atual será criado antes. Continuar?", "Restaurar"))
            return;

        try
        {
            await _backup.RestaurarBackupAsync(caminho);
            await _dialogs.InformarAsync("Backup restaurado",
                "Os dados foram restaurados. Feche e abra o aplicativo para carregar as informações restauradas.");
            MensagemStatus = "Backup restaurado. Reinicie o aplicativo.";
        }
        catch (Exception excecao)
        {
            MensagemStatus = $"Falha ao restaurar: {excecao.Message}";
        }
    }

    [RelayCommand]
    private void Voltar() => _navigation.IrParaInicio();

    private void AtualizarListaBackups()
    {
        BackupsDisponiveis.Clear();
        foreach (var caminho in _backup.ListarBackupsLocais().Take(10))
            BackupsDisponiveis.Add(Path.GetFileName(caminho));
    }
}
