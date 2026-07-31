using AuditorFiscal.Application.Interfaces.Persistence;
using AuditorFiscal.Domain.Entities;

namespace AuditorFiscal.Persistence.Repositories;

public class TagRepository(AuditorFiscalDbContext contexto) : Repository<Tag>(contexto), ITagRepository;
