using AuditorFiscal.Persistence.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuditorFiscal.Persistence;

/// <summary>
/// Usada apenas pela ferramenta `dotnet ef migrations add` em tempo de design.
/// A chave aqui NUNCA é a chave mestra protegida por DPAPI usada em produção —
/// é uma chave fixa de desenvolvimento, só para permitir abrir o arquivo SQLCipher
/// localmente ao gerar migrations.
/// </summary>
public class AuditorFiscalDbContextFactory : IDesignTimeDbContextFactory<AuditorFiscalDbContext>
{
    public AuditorFiscalDbContext CreateDbContext(string[] args)
    {
        var chaveDev = Environment.GetEnvironmentVariable("AUDITORFISCAL_DEV_DB_KEY") is { Length: > 0 } valor
            ? Convert.FromHexString(valor)
            : new byte[32];

        var connectionString = SqliteConnectionStringFactory.Criar("auditorfiscal.design.db", chaveDev);

        var optionsBuilder = new DbContextOptionsBuilder<AuditorFiscalDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new AuditorFiscalDbContext(optionsBuilder.Options);
    }
}
