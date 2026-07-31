using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Infrastructure.Backup;
using AuditorFiscal.Infrastructure.Export;
using AuditorFiscal.Infrastructure.Preferences;
using AuditorFiscal.Infrastructure.Printing;
using AuditorFiscal.Infrastructure.Security;
using AuditorFiscal.Infrastructure.System;
using AuditorFiscal.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace AuditorFiscal.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IMasterKeyProvider>(_ => new DpapiMasterKeyProvider(AppPaths.Seguro));
        services.AddSingleton<IHashService, Sha256HashService>();
        services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();
        services.AddSingleton<IAttachmentStorageService>(sp => new FileSystemAttachmentStorageService(
            sp.GetRequiredService<IEncryptionService>(),
            sp.GetRequiredService<IHashService>(),
            AppPaths.Anexos));

        services.AddSingleton<IPreferencesService>(_ => new JsonPreferencesService(AppPaths.Config));
        services.AddSingleton<IAutoStartService, RegistryAutoStartService>();

        services.AddTransient<IPdfExportService, PdfExportService>();
        services.AddTransient<IExcelExportService, ExcelExportService>();
        services.AddTransient<IPrintService, PrintService>();
        services.AddTransient<IBackupService, BackupService>();

        return services;
    }
}
