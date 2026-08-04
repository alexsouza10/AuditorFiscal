using AuditorFiscal.Domain.Enums;
using AuditorFiscal.Domain.ValueObjects;
using AuditorFiscal.Shared;

namespace AuditorFiscal.Domain.Entities;

/// <summary>
/// As datas seguem o fluxo de fiscalização do SFIT (CLAUDE V2): Recebimento e Abertura
/// no sistema, prazos de NAD e NCO, elaboração dos autos e a data final. Elas alimentam
/// diretamente o Cronograma GANTT, não apenas um único agendamento pontual.
/// </summary>
public class OrdemServico : EntidadeBase
{
    public string Numero { get; private set; } = string.Empty;
    public string Empresa { get; private set; } = string.Empty;
    public Cnpj Cnpj { get; private set; } = null!;
    public string Endereco { get; private set; } = string.Empty;
    public string Cidade { get; private set; } = string.Empty;

    /// <summary>Opcional: nem toda O.S. tem um responsável identificado no momento do cadastro.</summary>
    public string? Responsavel { get; private set; }
    public SituacaoOS Situacao { get; private set; }
    public TipoFiscalizacao Fiscalizacao { get; private set; }

    public DateOnly RecebimentoSfit { get; private set; }
    public DateOnly AberturaSfit { get; private set; }
    public DateOnly DataFiscalizacao { get; private set; }
    public DateOnly PrazoNad { get; private set; }
    public DateOnly PrazoNco { get; private set; }
    public DateOnly ElaboracaoAutos { get; private set; }
    public DateOnly DataFinal { get; private set; }

    public string? Observacoes { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public bool Favorito { get; private set; }

    /// <summary>
    /// NCRE: prazo-limite adicional (fora do fluxo SFIT padrão) para cumprir a ordem.
    /// Opcional — quando não cadastrado, o Gantt exibe um alerta lembrando o auditor.
    /// </summary>
    public bool TemNcre { get; private set; }
    public DateOnly? PrazoNcre { get; private set; }

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
        string? responsavel,
        TipoFiscalizacao fiscalizacao,
        DateOnly recebimentoSfit,
        DateOnly aberturaSfit,
        DateOnly dataFiscalizacao,
        DateOnly prazoNad,
        DateOnly prazoNco,
        DateOnly elaboracaoAutos,
        DateOnly dataFinal,
        DateTimeOffset momento,
        string? observacoes = null,
        Coordenada? coordenada = null)
    {
        Numero = Guard.NotNullOrWhiteSpace(numero, nameof(numero));
        Empresa = Guard.NotNullOrWhiteSpace(empresa, nameof(empresa));
        Cnpj = Guard.NotNull(cnpj, nameof(cnpj));
        Endereco = Guard.NotNullOrWhiteSpace(endereco, nameof(endereco));
        Cidade = Guard.NotNullOrWhiteSpace(cidade, nameof(cidade));
        Responsavel = string.IsNullOrWhiteSpace(responsavel) ? null : responsavel.Trim();
        Fiscalizacao = fiscalizacao;
        RecebimentoSfit = recebimentoSfit;
        AberturaSfit = aberturaSfit;
        DataFiscalizacao = dataFiscalizacao;
        PrazoNad = prazoNad;
        PrazoNco = prazoNco;
        ElaboracaoAutos = elaboracaoAutos;
        DataFinal = dataFinal;
        Observacoes = observacoes;
        Latitude = coordenada?.Latitude;
        Longitude = coordenada?.Longitude;
        Situacao = SituacaoOS.EmAndamento;

        CriadoEm = momento;
        AtualizadoEm = momento;
        _timeline.Add(new TimelineEvento(Id, "Ordem de serviço criada.", momento));
    }

    public void AtualizarDados(
        string numero,
        string empresa,
        Cnpj cnpj,
        string endereco,
        string cidade,
        string? responsavel,
        TipoFiscalizacao fiscalizacao,
        DateOnly recebimentoSfit,
        DateOnly aberturaSfit,
        DateOnly dataFiscalizacao,
        DateOnly prazoNad,
        DateOnly prazoNco,
        DateOnly elaboracaoAutos,
        DateOnly dataFinal,
        string? observacoes,
        Coordenada? coordenada,
        DateTimeOffset momento)
    {
        var responsavelNormalizado = string.IsNullOrWhiteSpace(responsavel) ? null : responsavel.Trim();

        // Sem essa checagem, o auto-save do formulário reenviava os mesmos dados a cada
        // poucos segundos e lotava o histórico com entradas idênticas de "atualizado".
        var alteracoes = new List<string>();
        RegistrarSeMudou(alteracoes, "O.S. N°", Numero, numero);
        RegistrarSeMudou(alteracoes, "Empresa", Empresa, empresa);
        RegistrarSeMudou(alteracoes, "CNPJ", Cnpj.Formatado(), cnpj.Formatado());
        RegistrarSeMudou(alteracoes, "Endereço", Endereco, endereco);
        RegistrarSeMudou(alteracoes, "Cidade", Cidade, cidade);
        RegistrarSeMudou(alteracoes, "Responsável", Responsavel, responsavelNormalizado);
        RegistrarSeMudou(alteracoes, "Fiscalização", Fiscalizacao.ToString(), fiscalizacao.ToString());
        RegistrarSeMudou(alteracoes, "Recebimento SFIT", RecebimentoSfit.ToString("dd/MM/yyyy"), recebimentoSfit.ToString("dd/MM/yyyy"));
        RegistrarSeMudou(alteracoes, "Abertura SFIT", AberturaSfit.ToString("dd/MM/yyyy"), aberturaSfit.ToString("dd/MM/yyyy"));
        RegistrarSeMudou(alteracoes, "Data da fiscalização", DataFiscalizacao.ToString("dd/MM/yyyy"), dataFiscalizacao.ToString("dd/MM/yyyy"));
        RegistrarSeMudou(alteracoes, "Prazo NAD", PrazoNad.ToString("dd/MM/yyyy"), prazoNad.ToString("dd/MM/yyyy"));
        RegistrarSeMudou(alteracoes, "Prazo NCO", PrazoNco.ToString("dd/MM/yyyy"), prazoNco.ToString("dd/MM/yyyy"));
        RegistrarSeMudou(alteracoes, "Elaboração dos autos", ElaboracaoAutos.ToString("dd/MM/yyyy"), elaboracaoAutos.ToString("dd/MM/yyyy"));
        RegistrarSeMudou(alteracoes, "Data final", DataFinal.ToString("dd/MM/yyyy"), dataFinal.ToString("dd/MM/yyyy"));
        RegistrarSeMudou(alteracoes, "Observações", Observacoes, observacoes);
        RegistrarSeMudou(alteracoes, "Latitude", Latitude?.ToString("F6"), coordenada?.Latitude.ToString("F6"));
        RegistrarSeMudou(alteracoes, "Longitude", Longitude?.ToString("F6"), coordenada?.Longitude.ToString("F6"));

        if (alteracoes.Count == 0)
            return;

        Numero = Guard.NotNullOrWhiteSpace(numero, nameof(numero));
        Empresa = Guard.NotNullOrWhiteSpace(empresa, nameof(empresa));
        Cnpj = Guard.NotNull(cnpj, nameof(cnpj));
        Endereco = Guard.NotNullOrWhiteSpace(endereco, nameof(endereco));
        Cidade = Guard.NotNullOrWhiteSpace(cidade, nameof(cidade));
        Responsavel = responsavelNormalizado;
        Fiscalizacao = fiscalizacao;
        RecebimentoSfit = recebimentoSfit;
        AberturaSfit = aberturaSfit;
        DataFiscalizacao = dataFiscalizacao;
        PrazoNad = prazoNad;
        PrazoNco = prazoNco;
        ElaboracaoAutos = elaboracaoAutos;
        DataFinal = dataFinal;
        Observacoes = observacoes;
        Latitude = coordenada?.Latitude;
        Longitude = coordenada?.Longitude;

        MarcarAtualizado(momento);
        _timeline.Add(new TimelineEvento(Id, string.Join(" | ", alteracoes), momento));
    }

    private static void RegistrarSeMudou(List<string> alteracoes, string campo, string? valorAntigo, string? valorNovo)
    {
        if (valorAntigo == valorNovo)
            return;

        var de = string.IsNullOrWhiteSpace(valorAntigo) ? "(vazio)" : valorAntigo;
        var para = string.IsNullOrWhiteSpace(valorNovo) ? "(vazio)" : valorNovo;
        alteracoes.Add($"{campo} alterado de \"{de}\" para \"{para}\"");
    }

    public void AlterarSituacao(SituacaoOS novaSituacao, DateTimeOffset momento)
    {
        if (Situacao == novaSituacao)
            return;

        Situacao = novaSituacao;
        MarcarAtualizado(momento);
        _timeline.Add(new TimelineEvento(Id, $"Situação alterada para {novaSituacao}.", momento));
    }

    /// <summary>Desloca todo o cronograma em N dias, preservando a duração de cada etapa.</summary>
    public void Adiar(int dias, DateTimeOffset momento)
    {
        if (dias == 0)
            return;

        RecebimentoSfit = RecebimentoSfit.AddDays(dias);
        AberturaSfit = AberturaSfit.AddDays(dias);
        DataFiscalizacao = DataFiscalizacao.AddDays(dias);
        PrazoNad = PrazoNad.AddDays(dias);
        PrazoNco = PrazoNco.AddDays(dias);
        ElaboracaoAutos = ElaboracaoAutos.AddDays(dias);
        DataFinal = DataFinal.AddDays(dias);

        MarcarAtualizado(momento);
        _timeline.Add(new TimelineEvento(Id, $"Cronograma adiado em {dias} dia(s).", momento));
    }

    public void DefinirNcre(bool temNcre, DateOnly? prazoNcre, DateTimeOffset momento)
    {
        TemNcre = temNcre;
        PrazoNcre = temNcre ? prazoNcre : null;
        MarcarAtualizado(momento);

        _timeline.Add(new TimelineEvento(Id, temNcre
            ? $"NCRE cadastrado — prazo em {prazoNcre:dd/MM/yyyy}."
            : "NCRE removido.", momento));
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

    /// <summary>Ainda em curso: é destas O.S. que cobramos prazo e movimentação, não das já encerradas.</summary>
    public bool Ativa => Situacao is SituacaoOS.Agendada or SituacaoOS.EmAndamento or SituacaoOS.Adiada;

    /// <summary>A data final planejada já passou e a O.S. segue aberta — deveria ter sido concluída.</summary>
    public bool EstaAtrasada(DateOnly referencia) => Ativa && DataFinal < referencia;

    public int DiasAtraso(DateOnly referencia) => EstaAtrasada(referencia) ? referencia.DayNumber - DataFinal.DayNumber : 0;

    /// <summary>Dias desde a última alteração registrada — sinaliza O.S. esquecidas, não só atrasadas.</summary>
    public int DiasSemMovimentacao(DateOnly referencia) =>
        Ativa ? Math.Max(0, referencia.DayNumber - DateOnly.FromDateTime(AtualizadoEm.LocalDateTime).DayNumber) : 0;

    /// <summary>
    /// Checkpoints de prazo (NAD, NCO, data final) que caem dentro da janela de alerta — usado
    /// para avisos granulares tipo "Prazo NAD vence em 5 dias". Cada etapa é avaliada de forma
    /// independente porque perder o NAD não significa que o NCO deixou de valer.
    /// </summary>
    public IReadOnlyList<(string Etapa, DateOnly Data)> PrazosProximos(DateOnly referencia, int diasJanela)
    {
        if (!Ativa)
            return [];

        (string Etapa, DateOnly Data)[] checkpoints = [("Prazo NAD", PrazoNad), ("Prazo NCO", PrazoNco), ("Data final", DataFinal)];
        return checkpoints.Where(c => c.Data >= referencia && c.Data <= referencia.AddDays(diasJanela)).ToList();
    }

    /// <summary>
    /// Dias até o próximo checkpoint pendente (NAD, NCO ou data final), usado para ordenar e
    /// filtrar por urgência (ex.: "prazo&lt;5"). Negativo quando todos já passaram.
    /// </summary>
    public int? DiasProximoPrazo(DateOnly referencia)
    {
        if (!Ativa)
            return null;

        DateOnly[] checkpoints = [PrazoNad, PrazoNco, DataFinal];
        var proximo = checkpoints.Where(c => c >= referencia).OrderBy(c => c).Cast<DateOnly?>().FirstOrDefault() ?? DataFinal;
        return proximo.DayNumber - referencia.DayNumber;
    }
}
