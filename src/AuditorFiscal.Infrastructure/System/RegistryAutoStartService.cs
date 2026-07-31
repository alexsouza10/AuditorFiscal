using AuditorFiscal.Application.Interfaces.Services;
using Microsoft.Win32;

namespace AuditorFiscal.Infrastructure.System;

/// <summary>
/// Inicialização automática pela chave Run do usuário atual — não exige privilégio de
/// administrador e afeta apenas quem instalou o aplicativo.
/// </summary>
public class RegistryAutoStartService : IAutoStartService
{
    private const string CaminhoChave = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string NomeValor = "AuditorFiscal";

    public bool EstaHabilitado()
    {
        using var chave = Registry.CurrentUser.OpenSubKey(CaminhoChave, writable: false);
        return chave?.GetValue(NomeValor) is not null;
    }

    public void Habilitar()
    {
        // Em executável single-file, Assembly.Location vem vazio: só ProcessPath aponta
        // para o .exe real que o Windows precisa executar no logon.
        var caminhoExecutavel = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(caminhoExecutavel))
            return;

        using var chave = Registry.CurrentUser.OpenSubKey(CaminhoChave, writable: true);
        chave?.SetValue(NomeValor, $"\"{caminhoExecutavel}\"");
    }

    public void Desabilitar()
    {
        using var chave = Registry.CurrentUser.OpenSubKey(CaminhoChave, writable: true);
        chave?.DeleteValue(NomeValor, throwOnMissingValue: false);
    }
}
