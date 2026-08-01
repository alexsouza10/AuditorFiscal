using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
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

        ConfiarNoProprioCertificadoSeNecessario();

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            MostrarErroFatal(ex);
        }
    }

    /// <summary>
    /// Extrai o certificado que já assina este próprio .exe e tenta confiar nele
    /// automaticamente — sem isso, cada máquina nova mostraria "Editor desconhecido" e
    /// exigiria rodar um instalador à parte. Só tenta uma vez por máquina (marcador em
    /// AppPaths.Config): se a pessoa recusar a caixa nativa do Windows, respeita a escolha
    /// e não fica insistindo a cada abertura. Qualquer falha aqui é silenciosa — o app
    /// tem que abrir mesmo que essa confiança automática não funcione (o SmartScreen ainda
    /// permite "Executar assim mesmo" manualmente).
    /// </summary>
    private static void ConfiarNoProprioCertificadoSeNecessario()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.Config);
            var caminhoMarcador = Path.Combine(AppPaths.Config, ".confianca-certificado-tentada");
            if (File.Exists(caminhoMarcador))
                return;

            File.WriteAllText(caminhoMarcador, DateTime.Now.ToString("O"));

            var caminhoExe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(caminhoExe) || !File.Exists(caminhoExe))
                return;

            using var certificadoProprio = new X509Certificate2(X509Certificate.CreateFromSignedFile(caminhoExe));

            using var lojaLeitura = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            lojaLeitura.Open(OpenFlags.ReadOnly);
            var jaConfia = lojaLeitura.Certificates.Find(
                X509FindType.FindByThumbprint, certificadoProprio.Thumbprint, validOnly: false).Count > 0;
            lojaLeitura.Close();

            if (jaConfia)
                return;

            // Dispara a caixa nativa do Windows perguntando se a pessoa confia no
            // certificado — não tem como pular esse clique por segurança, mas é só uma
            // vez: da próxima abertura em diante (e em versões futuras assinadas com o
            // mesmo certificado) não pergunta de novo.
            using var lojaEscrita = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            lojaEscrita.Open(OpenFlags.ReadWrite);
            lojaEscrita.Add(certificadoProprio);
            lojaEscrita.Close();
        }
        catch
        {
            // Silencioso de propósito — ver o comentário do método.
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
