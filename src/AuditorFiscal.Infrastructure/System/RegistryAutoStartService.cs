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
    private const string NomeValor = "GERENCIADOR DE AFT";

    // Nome usado antes de o app se chamar "GERENCIADOR DE AFT" — removido ao habilitar/desabilitar
    // para quem atualiza a versão instalada não ficar com duas entradas de inicialização automática.
    private const string NomeValorLegado = "Apura Fiscal";

    public bool EstaHabilitado()
    {
        using var chave = Registry.CurrentUser.OpenSubKey(CaminhoChave, writable: false);
        return chave?.GetValue(NomeValor) is not null || chave?.GetValue(NomeValorLegado) is not null;
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
        chave?.DeleteValue(NomeValorLegado, throwOnMissingValue: false);
    }

    public void Desabilitar()
    {
        using var chave = Registry.CurrentUser.OpenSubKey(CaminhoChave, writable: true);
        chave?.DeleteValue(NomeValor, throwOnMissingValue: false);
        chave?.DeleteValue(NomeValorLegado, throwOnMissingValue: false);
    }
}
