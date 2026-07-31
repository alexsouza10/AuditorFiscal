using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AuditorFiscal.Infrastructure.Logging;

/// <summary>
/// Logs técnicos (Serilog) para diagnóstico, distintos do LogInterno (trilha de
/// auditoria visível na UI). Nunca deve receber CNPJ, endereço ou outros dados
/// sensíveis — somente ids, contagens e nomes de operação. O logging de comando
/// do EF Core é rebaixado para Warning: por padrão ele escreve o SQL de cada
/// operação em Information, o que só inflaria o arquivo de log em um app "leve".
/// </summary>
public static class LoggingConfig
{
    public static Logger CriarLogger(string pastaLogs)
    {
        Directory.CreateDirectory(pastaLogs);

        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(pastaLogs, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();
    }
}
