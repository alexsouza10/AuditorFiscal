using System.Reflection;

namespace AuditorFiscal.UI;

/// <summary>
/// Versão publicada pelo Makefile (a partir da última tag do git) e embutida no assembly
/// via "-p:Version=...". Fora de um publish oficial, cai no marcador de build de desenvolvimento
/// definido em Directory.Build.props.
/// </summary>
public static class AppVersion
{
    public static string Atual { get; } =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? "dev";
}
