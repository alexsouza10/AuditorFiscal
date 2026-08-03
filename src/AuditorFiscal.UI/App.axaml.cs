using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Application.OrdensServico;
using AuditorFiscal.Application.OrdensServico.Dtos;
using AuditorFiscal.Application.OrdensServico.Validators;
using AuditorFiscal.Infrastructure;
using AuditorFiscal.Infrastructure.Logging;
using AuditorFiscal.Infrastructure.Preferences;
using AuditorFiscal.Persistence;
using AuditorFiscal.Shared;
using AuditorFiscal.UI.Services;
using AuditorFiscal.UI.ViewModels;
using AuditorFiscal.UI.Views;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AuditorFiscal.UI;

public partial class App : global::Avalonia.Application
{
    private IHost? _host;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // O tema é lido antes do host porque a janela já nasce com a variante correta,
        // evitando o "flash" de tema claro em quem usa o app no escuro.
        var preferencias = new JsonPreferencesService(AppPaths.Config);
        RequestedThemeVariant = preferencias.Atual.Tema switch
        {
            TemaAplicacao.Claro => global::Avalonia.Styling.ThemeVariant.Light,
            TemaAplicacao.Escuro => global::Avalonia.Styling.ThemeVariant.Dark,
            _ => global::Avalonia.Styling.ThemeVariant.Default
        };
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Log.Logger = LoggingConfig.CriarLogger(AppPaths.Logs);

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(ConfigurarServicos)
            .Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            AplicarRestauracaoPendente();
            AplicarMigracoes();

            desktop.MainWindow = new MainWindow
            {
                DataContext = _host.Services.GetRequiredService<MainViewModel>()
            };

            _ = ExecutarBackupAutomaticoAsync();

            desktop.ShutdownRequested += (_, _) =>
            {
                // Bloqueia a saída propositalmente: o backup de fechamento só é útil se
                // terminar antes do host (e a conexão com o banco) serem derrubados.
                Task.Run(ExecutarBackupAutomaticoAsync).GetAwaiter().GetResult();

                Log.CloseAndFlush();
                _host.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigurarServicos(IServiceCollection services)
    {
        services.AddPersistence(AppPaths.BancoDados);
        services.AddInfrastructure();

        services.AddTransient<IValidator<CriarOrdemServicoDto>, CriarOrdemServicoDtoValidator>();
        services.AddTransient<IValidator<AtualizarOrdemServicoDto>, AtualizarOrdemServicoDtoValidator>();
        services.AddTransient<OrdemServicoService>();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IFilePickerService, AvaloniaFilePickerService>();
        services.AddSingleton<IFileDialogService, AvaloniaFileDialogService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IThemeService, ThemeService>();

        services.AddSingleton<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<GanttViewModel>();
        services.AddTransient<BancoDadosViewModel>();
        services.AddTransient<OrdemServicoFormViewModel>();
        services.AddTransient<ConfiguracoesViewModel>();
        services.AddTransient<DashboardViewModel>();
    }

    private void AplicarMigracoes()
    {
        using var escopo = _host!.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<AuditorFiscalDbContext>();
        contexto.Database.Migrate();

        // Trocar de "wal" para "delete" exige acesso exclusivo ao banco — só garantido
        // aqui, no único momento em que nenhuma outra conexão do app ainda foi aberta.
        // Sem isso, um backup feito logo após uma alteração podia não refletir essa
        // alteração: em modo WAL, o commit vai primeiro para o arquivo .db-wal e só é
        // mesclado no .db principal (o único arquivo que o backup empacota) depois —
        // às vezes bem depois. Rodar isso a cada início do app também corrige bancos
        // restaurados de um backup antigo que ainda estivesse em modo WAL.
        contexto.Database.ExecuteSqlRaw("PRAGMA journal_mode=DELETE;");
    }

    /// <summary>
    /// Precisa rodar antes de qualquer conexão com o banco ser aberta: uma restauração
    /// pendente sobrescreve o arquivo do banco, o que falharia (ou corromperia dados) se
    /// já houvesse uma conexão ativa apontando para ele.
    /// </summary>
    private void AplicarRestauracaoPendente()
    {
        var backup = _host!.Services.GetRequiredService<IBackupService>();

        // Task.Run tira a chamada do contexto de sincronização da UI antes de bloquear:
        // sem isso, os "await" internos tentariam retomar na mesma thread da UI que está
        // bloqueada aqui esperando o resultado — um deadlock clássico que travava a
        // abertura do app.
        Task.Run(() => backup.AplicarRestauracaoPendenteAsync()).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Backup automático ao abrir e ao fechar o app: nunca deve derrubar o app se o disco
    /// estiver indisponível. Sempre grava por cima do mesmo arquivo (ver
    /// <see cref="AuditorFiscal.Infrastructure.Backup.BackupService"/>), então rodar em toda
    /// abertura/fechamento não acumula backups antigos na pasta.
    /// </summary>
    private async Task ExecutarBackupAutomaticoAsync()
    {
        try
        {
            var preferencias = _host!.Services.GetRequiredService<IPreferencesService>();
            if (!preferencias.Atual.BackupAutomatico)
                return;

            var backup = _host.Services.GetRequiredService<IBackupService>();
            var registro = await backup.CriarBackupAsync(automatico: true);

            Log.Information("Backup automático concluído ({Bytes} bytes).", registro.TamanhoBytes);
        }
        catch (Exception excecao)
        {
            Log.Warning(excecao, "Falha no backup automático.");
        }
    }
}
