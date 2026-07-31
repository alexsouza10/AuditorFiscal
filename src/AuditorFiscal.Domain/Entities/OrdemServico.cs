using AuditorFiscal.Domain.Enums;
using AuditorFiscal.Domain.ValueObjects;
using AuditorFiscal.Shared;

namespace AuditorFiscal.Domain.Entities;

public class OrdemServico : EntidadeBase
{
    public string Numero { get; private set; } = string.Empty;
    public string Empresa { get; private set; } = string.Empty;
    public Cnpj Cnpj { get; private set; } = null!;
    public string Endereco { get; private set; } = string.Empty;
    public string Cidade { get; private set; } = string.Empty;
    public string Responsavel { get; private set; } = string.Empty;
    public DateOnly Data { get; private set; }
    public TimeOnly Hora { get; private set; }
    public SituacaoOS Situacao { get; private set; }
    public Guid TipoAuditoriaId { get; private set; }
    public TipoAuditoria? TipoAuditoria { get; private set; }
    public string? Observacoes { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public bool Favorito { get; private set; }

    /// <summary>
    /// Projeção somente leitura das colunas Latitude/Longitude. As coordenadas ficam como
    /// colunas simples e anuláveis em vez de owned type: um owned opcional com table
    /// splitting quebra o UPDATE quando está nulo, que é o caso mais comum aqui.
    /// </summary>
    public Coordenada? Coordenada =>
        Latitude.HasValue && Longitude.HasValue ? new Coordenada(Latitude.Value, Longitude.Value) : null;
    public string HashIntegridade { get; private set; } = string.Empty;

    private readonly List<Foto> _fotos = [];
    public IReadOnlyCollection<Foto> Fotos => _fotos.AsReadOnly();

    private readonly List<Anexo> _anexos = [];
    public IReadOnlyCollection<Anexo> Anexos => _anexos.AsReadOnly();

    private readonly List<TimelineEvento> _timeline = [];
    public IReadOnlyCollection<TimelineEvento> Timeline => _timeline.AsReadOnly();

    private readonly List<Tag> _tags = [];
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();

    private OrdemServico()
    {
    }

    public OrdemServico(
        string numero,
        string empresa,
        Cnpj cnpj,
        string endereco,
        string cidade,
        string responsavel,
        DateOnly data,
        TimeOnly hora,
        Guid tipoAuditoriaId,
        DateTimeOffset momento,
        string? observacoes = null,
        Coordenada? coordenada = null)
    {
        Numero = Guard.NotNullOrWhiteSpace(numero, nameof(numero));
        Empresa = Guard.NotNullOrWhiteSpace(empresa, nameof(empresa));
        Cnpj = Guard.NotNull(cnpj, nameof(cnpj));
        Endereco = Guard.NotNullOrWhiteSpace(endereco, nameof(endereco));
        Cidade = Guard.NotNullOrWhiteSpace(cidade, nameof(cidade));
        Responsavel = Guard.NotNullOrWhiteSpace(responsavel, nameof(responsavel));
        Data = data;
        Hora = hora;
        TipoAuditoriaId = tipoAuditoriaId;
        Observacoes = observacoes;
        Latitude = coordenada?.Latitude;
        Longitude = coordenada?.Longitude;
        Situacao = SituacaoOS.Agendada;

        CriadoEm = momento;
        AtualizadoEm = momento;
        _timeline.Add(new TimelineEvento(Id, "Ordem de serviço criada.", momento));
    }

    public void AtualizarDados(
        string empresa,
        Cnpj cnpj,
        string endereco,
        string cidade,
        string responsavel,
        DateOnly data,
        TimeOnly hora,
        Guid tipoAuditoriaId,
        string? observacoes,
        Coordenada? coordenada,
        DateTimeOffset momento)
    {
        Empresa = Guard.NotNullOrWhiteSpace(empresa, nameof(empresa));
        Cnpj = Guard.NotNull(cnpj, nameof(cnpj));
        Endereco = Guard.NotNullOrWhiteSpace(endereco, nameof(endereco));
        Cidade = Guard.NotNullOrWhiteSpace(cidade, nameof(cidade));
        Responsavel = Guard.NotNullOrWhiteSpace(responsavel, nameof(responsavel));
        Data = data;
        Hora = hora;
        TipoAuditoriaId = tipoAuditoriaId;
        Observacoes = observacoes;
        Latitude = coordenada?.Latitude;
        Longitude = coordenada?.Longitude;

        MarcarAtualizado(momento);
        _timeline.Add(new TimelineEvento(Id, "Dados da ordem de serviço atualizados.", momento));
    }

    public void AlterarSituacao(SituacaoOS novaSituacao, DateTimeOffset momento)
    {
        if (Situacao == novaSituacao)
            return;

        Situacao = novaSituacao;
        MarcarAtualizado(momento);
        _timeline.Add(new TimelineEvento(Id, $"Situação alterada para {novaSituacao}.", momento));
    }

    public void Reagendar(DateOnly data, TimeOnly hora, DateTimeOffset momento)
    {
        if (Data == data && Hora == hora)
            return;

        Data = data;
        Hora = hora;
        MarcarAtualizado(momento);
        _timeline.Add(new TimelineEvento(Id, $"Reagendada para {data:dd/MM/yyyy} às {hora:HH\\:mm}.", momento));
    }

    public void AlternarFavorito() => Favorito = !Favorito;

    public void AdicionarFoto(Foto foto)
    {
        Guard.NotNull(foto, nameof(foto));
        _fotos.Add(foto);
    }

    public void RemoverFoto(Guid fotoId) => _fotos.RemoveAll(f => f.Id == fotoId);

    public void AdicionarAnexo(Anexo anexo)
    {
        Guard.NotNull(anexo, nameof(anexo));
        _anexos.Add(anexo);
    }

    public void RemoverAnexo(Guid anexoId) => _anexos.RemoveAll(a => a.Id == anexoId);

    public void AdicionarTag(Tag tag)
    {
        Guard.NotNull(tag, nameof(tag));
        if (_tags.All(t => t.Id != tag.Id))
            _tags.Add(tag);
    }

    public void RemoverTag(Guid tagId) => _tags.RemoveAll(t => t.Id == tagId);

    public void DefinirHashIntegridade(string hash) => HashIntegridade = Guard.NotNullOrWhiteSpace(hash, nameof(hash));
}
