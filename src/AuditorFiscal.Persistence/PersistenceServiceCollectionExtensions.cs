using AuditorFiscal.Application.Interfaces.Persistence;
using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Persistence.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuditorFiscal.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string caminhoBancoDados)
    {
        // Transient (não Scoped): a UI Avalonia não tem um "escopo por requisição" como
        // uma aplicação web. Um DbContext por ViewModel resolvido evita tanto acumular
        // rastreamento de entidades por toda a sessão do app quanto o erro de resolver
        // um serviço Scoped a partir do provedor raiz do host.
        services.AddDbContext<AuditorFiscalDbContext>((sp, options) =>
        {
            var chave = sp.GetRequiredService<IMasterKeyProvider>().ObterChave();
            var connectionString = SqliteConnectionStringFactory.Criar(caminhoBancoDados, chave);
            options.UseSqlite(connectionString);
        }, contextLifetime: ServiceLifetime.Transient, optionsLifetime: ServiceLifetime.Singleton);

        services.AddTransient<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
