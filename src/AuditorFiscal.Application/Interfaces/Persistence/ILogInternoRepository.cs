using AuditorFiscal.Domain.Entities;

namespace AuditorFiscal.Application.Interfaces.Persistence;

public interface ILogInternoRepository : IRepository<LogInterno>
{
    Task<IReadOnlyList<LogInterno>> ListarRecentesAsync(int quantidade, CancellationToken ct = default);
}
