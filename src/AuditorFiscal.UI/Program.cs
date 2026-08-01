using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using AuditorFiscal.Shared;

namespace AuditorFiscal.UI;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Sem isso, uma falha na inicialização (antes até do Serilog existir) derrubava o
        // processo em silêncio: era um WinExe sem console, então a pessoa só via "não abre
        // nada", sem forma de saber o motivo. O MessageBox nativo funciona mesmo se o
        // Avalonia ou o host de DI nunca chegarem a inicializar.
        AppDomain.CurrentDomain.UnhandledException += (_, e) => MostrarErroFatal(e.ExceptionObject as Exception);

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            MostrarErroFatal(ex);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    private static void MostrarErroFatal(Exception? excecao)
    {
        var caminhoLog = "(não foi possível gravar o log)";
        try
        {
            Directory.CreateDirectory(AppPaths.Logs);
            caminhoLog = Path.Combine(AppPaths.Logs, "erro-fatal.txt");
            File.WriteAllText(caminhoLog, $"{DateTime.Now:dd/MM/yyyy HH:mm:ss}{Environment.NewLine}{excecao}");
        }
        catch
        {
            // Se nem isso funcionar (ex.: disco sem espaço, permissão negada), segue só
            // com o MessageBox — não pode lançar de dentro do handler de erro fatal.
        }

        MessageBox(
            IntPtr.Zero,
            $"O Auditor Fiscal encontrou um erro ao iniciar e será fechado.{Environment.NewLine}{Environment.NewLine}" +
            $"{excecao?.GetType().Name}: {excecao?.Message}{Environment.NewLine}{Environment.NewLine}" +
            $"Detalhes completos salvos em:{Environment.NewLine}{caminhoLog}",
            "Auditor Fiscal — Erro ao iniciar",
            0x00000010 /* MB_ICONERROR */);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}
