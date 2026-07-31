using AuditorFiscal.Application.Interfaces.Persistence;
using AuditorFiscal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditorFiscal.Persistence.Repositories;

public class Repository<T>(AuditorFiscalDbContext contexto) : IRepository<T> where T : EntidadeBase
{
    protected readonly AuditorFiscalDbContext Contexto = contexto;
    protected readonly DbSet<T> DbSet = contexto.Set<T>();

    public virtual async Task<T?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(x => x.Id == id, ct);

    public virtual async Task<IReadOnlyList<T>> ListarAsync(CancellationToken ct = default) =>
        await DbSet.ToListAsync(ct);

    public virtual async Task AdicionarAsync(T entidade, CancellationToken ct = default) =>
        await DbSet.AddAsync(entidade, ct);

    public virtual void Remover(T entidade) => DbSet.Remove(entidade);
}
