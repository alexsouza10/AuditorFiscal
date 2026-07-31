using AuditorFiscal.Domain.Entities;

namespace AuditorFiscal.Application.Interfaces.Persistence;

public interface IRepository<T> where T : EntidadeBase
{
    Task<T?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListarAsync(CancellationToken ct = default);
    Task AdicionarAsync(T entidade, CancellationToken ct = default);
    void Remover(T entidade);
}
