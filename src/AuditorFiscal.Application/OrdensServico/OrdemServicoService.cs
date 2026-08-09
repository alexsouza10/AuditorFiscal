using AuditorFiscal.Application.Interfaces.Persistence;
using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Application.OrdensServico.Dtos;
using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;
using AuditorFiscal.Domain.ValueObjects;
using AuditorFiscal.Shared;
using FluentValidation;

namespace AuditorFiscal.Application.OrdensServico;

public class OrdemServicoService(
    IUnitOfWork unitOfWork,
    IValidator<CriarOrdemServicoDto> criarValidator,
    IValidator<AtualizarOrdemServicoDto> atualizarValidator,
    IHashService hashService,
    IAttachmentStorageService attachmentStorage,
    IClock clock)
{
    public async Task<Result<Guid>> CriarAsync(
        CriarOrdemServicoDto dto,
        IReadOnlyList<NovoArquivoDto>? arquivos = null,
        CancellationToken ct = default)
    {
        var validacao = await criarValidator.ValidateAsync(dto, ct);
        if (!validacao.IsValid)
            return Result<Guid>.Failure(MensagemDeErro(validacao.Errors));

        if (await unitOfWork.OrdensServico.NumeroJaExisteAsync(dto.Numero, null, ct))
            return Result<Guid>.Failure($"Já existe uma ordem de serviço com o número '{dto.Numero}'.");

        var ordemServico = new OrdemServico(
            dto.Numero,
            dto.Empresa,
            Cnpj.Criar(dto.Cnpj),
            dto.Endereco,
            dto.Cidade,
            dto.Responsavel,
            dto.Fiscalizacao,
            dto.RecebimentoSfit,
            dto.AberturaSfit,
            dto.DataFiscalizacao,
            dto.PrazoNad,
            dto.PrazoNco,
            dto.ElaboracaoAutos,
            dto.DataFinal,
            clock.UtcNow,
            dto.Observacoes,
            CriarCoordenada(dto.Latitude, dto.Longitude),
            dto.PapelAuditor);

        if (dto.TemNcre)
            ordemServico.DefinirNcre(true, dto.NcreInicio, dto.NcreFim, clock.UtcNow);

        await AnexarArquivosAsync(ordemServico, arquivos, ct);
        AtualizarHashIntegridade(ordemServico);

        await unitOfWork.OrdensServico.AdicionarAsync(ordemServico, ct);
        await RegistrarLogAsync("Ordem de serviço criada", ordemServico.Numero, ct);
        await unitOfWork.SalvarAlteracoesAsync(ct);

        return Result<Guid>.Success(ordemServico.Id);
    }

    public async Task<Result> AtualizarAsync(
        AtualizarOrdemServicoDto dto,
        IReadOnlyList<NovoArquivoDto>? novosArquivos = null,
        CancellationToken ct = default)
    {
        var validacao = await atualizarValidator.ValidateAsync(dto, ct);
        if (!validacao.IsValid)
            return Result.Failure(MensagemDeErro(validacao.Errors));

        var ordemServico = await unitOfWork.OrdensServico.ObterComDetalhesAsync(dto.Id, ct);
        if (ordemServico is null)
            return Result.Failure("Ordem de serviço não encontrada.");

        if (dto.Numero != ordemServico.Numero &&
            await unitOfWork.OrdensServico.NumeroJaExisteAsync(dto.Numero, dto.Id, ct))
            return Result.Failure($"Já existe uma ordem de serviço com o número '{dto.Numero}'.");

        ordemServico.AtualizarDados(
            dto.Numero,
            dto.Empresa,
            Cnpj.Criar(dto.Cnpj),
            dto.Endereco,
            dto.Cidade,
            dto.Responsavel,
            dto.Fiscalizacao,
            dto.PapelAuditor,
            dto.RecebimentoSfit,
            dto.AberturaSfit,
            dto.DataFiscalizacao,
            dto.PrazoNad,
            dto.PrazoNco,
            dto.ElaboracaoAutos,
            dto.DataFinal,
            dto.Observacoes,
            CriarCoordenada(dto.Latitude, dto.Longitude),
            clock.UtcNow);

        if (dto.TemNcre != ordemServico.TemNcre || dto.NcreInicio != ordemServico.NcreInicio || dto.NcreFim != ordemServico.NcreFim)
            ordemServico.DefinirNcre(dto.TemNcre, dto.NcreInicio, dto.NcreFim, clock.UtcNow);

        await AnexarArquivosAsync(ordemServico, novosArquivos, ct);
        AtualizarHashIntegridade(ordemServico);
        await unitOfWork.SalvarAlteracoesAsync(ct);

        return Result.Success();
    }

    public Task<OrdemServico?> ObterDetalheAsync(Guid id, CancellationToken ct = default) =>
        unitOfWork.OrdensServico.ObterComDetalhesAsync(id, ct);

    public async Task<Result> AlterarSituacaoAsync(Guid id, SituacaoOS situacao, CancellationToken ct = default)
    {
        var ordemServico = await unitOfWork.OrdensServico.ObterComDetalhesAsync(id, ct);
        if (ordemServico is null)
            return Result.Failure("Ordem de serviço não encontrada.");

        ordemServico.AlterarSituacao(situacao, clock.UtcNow);
        AtualizarHashIntegridade(ordemServico);
        await RegistrarLogAsync($"Situação alterada para {situacao.Descricao()}", ordemServico.Numero, ct);
        await unitOfWork.SalvarAlteracoesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> AdiarAsync(Guid id, int dias, CancellationToken ct = default)
    {
        var ordemServico = await unitOfWork.OrdensServico.ObterComDetalhesAsync(id, ct);
        if (ordemServico is null)
            return Result.Failure("Ordem de serviço não encontrada.");

        ordemServico.Adiar(dias, clock.UtcNow);
        AtualizarHashIntegridade(ordemServico);
        await unitOfWork.SalvarAlteracoesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> AlternarFavoritoAsync(Guid id, CancellationToken ct = default)
    {
        var ordemServico = await unitOfWork.OrdensServico.ObterPorIdAsync(id, ct);
        if (ordemServico is null)
            return Result.Failure("Ordem de serviço não encontrada.");

        ordemServico.AlternarFavorito();
        await unitOfWork.SalvarAlteracoesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        var ordemServico = await unitOfWork.OrdensServico.ObterComDetalhesAsync(id, ct);
        if (ordemServico is null)
            return Result.Failure("Ordem de serviço não encontrada.");

        var numero = ordemServico.Numero;

        foreach (var caminho in ordemServico.Fotos.Select(f => f.CaminhoArmazenamento)
                     .Concat(ordemServico.Anexos.Select(a => a.CaminhoArmazenamento))
                     .ToList())
            attachmentStorage.Excluir(caminho);

        unitOfWork.OrdensServico.Remover(ordemServico);
        await RegistrarLogAsync("Ordem de serviço excluída", numero, ct);
        await unitOfWork.SalvarAlteracoesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> RemoverArquivoAsync(Guid ordemServicoId, Guid arquivoId, CancellationToken ct = default)
    {
        var ordemServico = await unitOfWork.OrdensServico.ObterComDetalhesAsync(ordemServicoId, ct);
        if (ordemServico is null)
            return Result.Failure("Ordem de serviço não encontrada.");

        var caminho = ordemServico.Fotos.FirstOrDefault(f => f.Id == arquivoId)?.CaminhoArmazenamento
                      ?? ordemServico.Anexos.FirstOrDefault(a => a.Id == arquivoId)?.CaminhoArmazenamento;

        if (caminho is null)
            return Result.Failure("Arquivo não encontrado.");

        ordemServico.RemoverFoto(arquivoId);
        ordemServico.RemoverAnexo(arquivoId);
        attachmentStorage.Excluir(caminho);
        await unitOfWork.SalvarAlteracoesAsync(ct);

        return Result.Success();
    }

    public Task<byte[]> AbrirArquivoAsync(string caminhoArmazenamento, CancellationToken ct = default) =>
        attachmentStorage.AbrirDecriptografadoAsync(caminhoArmazenamento, ct);

    public Task<IReadOnlyList<OrdemServico>> BuscarAsync(FiltroOrdemServicoDto filtro, CancellationToken ct = default) =>
        unitOfWork.OrdensServico.BuscarAsync(filtro, ct);

    public Task<IReadOnlyList<OrdemServico>> ObterPorPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken ct = default) =>
        unitOfWork.OrdensServico.ObterPorPeriodoAsync(inicio, fim, ct);

    public Task<IReadOnlyList<OrdemServico>> ObterPorEmpresaAsync(string empresa, CancellationToken ct = default) =>
        unitOfWork.OrdensServico.ObterPorEmpresaAsync(empresa, ct);

    public Task<IReadOnlyList<string>> ListarEmpresasAsync(CancellationToken ct = default) =>
        unitOfWork.OrdensServico.ListarEmpresasAsync(ct);

    public Task<string> SugerirProximoNumeroAsync(CancellationToken ct = default) =>
        unitOfWork.OrdensServico.SugerirProximoNumeroAsync(ct);

    public Task<IReadOnlyList<Tag>> ListarTagsAsync(CancellationToken ct = default) =>
        unitOfWork.Tags.ListarAsync(ct);

    public async Task<Result<Guid>> CriarTagAsync(string nome, string cor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result<Guid>.Failure("Informe o nome da tag.");

        var existentes = await unitOfWork.Tags.ListarAsync(ct);
        if (existentes.Any(t => string.Equals(t.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Result<Guid>.Failure("Já existe uma tag com esse nome.");

        var tag = new Tag(nome.Trim(), cor);
        await unitOfWork.Tags.AdicionarAsync(tag, ct);
        await unitOfWork.SalvarAlteracoesAsync(ct);

        return Result<Guid>.Success(tag.Id);
    }

    public async Task<Result> DefinirTagsAsync(Guid ordemServicoId, IReadOnlyList<Guid> tagIds, CancellationToken ct = default)
    {
        var ordemServico = await unitOfWork.OrdensServico.ObterComDetalhesAsync(ordemServicoId, ct);
        if (ordemServico is null)
            return Result.Failure("Ordem de serviço não encontrada.");

        var todasTags = await unitOfWork.Tags.ListarAsync(ct);

        foreach (var tagAtual in ordemServico.Tags.Where(t => !tagIds.Contains(t.Id)).ToList())
            ordemServico.RemoverTag(tagAtual.Id);

        foreach (var tag in todasTags.Where(t => tagIds.Contains(t.Id)))
            ordemServico.AdicionarTag(tag);

        await unitOfWork.SalvarAlteracoesAsync(ct);
        return Result.Success();
    }

    public Task<IReadOnlyList<LogInterno>> ListarLogsAsync(int quantidade, CancellationToken ct = default) =>
        unitOfWork.Logs.ListarRecentesAsync(quantidade, ct);

    public async Task RegistrarLogAsync(string acao, string? detalhes = null, CancellationToken ct = default)
    {
        await unitOfWork.Logs.AdicionarAsync(new LogInterno(acao, clock.UtcNow, detalhes), ct);
    }

    private async Task AnexarArquivosAsync(OrdemServico ordemServico, IReadOnlyList<NovoArquivoDto>? arquivos, CancellationToken ct)
    {
        if (arquivos is null || arquivos.Count == 0)
            return;

        foreach (var arquivo in arquivos)
        {
            var armazenado = await attachmentStorage.SalvarAsync(
                ordemServico.Id, arquivo.NomeArquivo, arquivo.Conteudo, ct);

            if (arquivo.Tipo == TipoArquivo.Foto)
                ordemServico.AdicionarFoto(new Foto(
                    ordemServico.Id, arquivo.NomeArquivo, arquivo.ContentType,
                    armazenado.CaminhoArmazenamento, armazenado.TamanhoBytes, armazenado.HashSha256, clock.UtcNow));
            else
                ordemServico.AdicionarAnexo(new Anexo(
                    ordemServico.Id, arquivo.NomeArquivo, arquivo.ContentType,
                    armazenado.CaminhoArmazenamento, armazenado.TamanhoBytes, armazenado.HashSha256));
        }
    }

    private void AtualizarHashIntegridade(OrdemServico ordemServico)
    {
        var conteudo = string.Join('|',
            ordemServico.Numero, ordemServico.Empresa, ordemServico.Cnpj.Numero, ordemServico.Endereco,
            ordemServico.Cidade, ordemServico.Responsavel, ordemServico.Fiscalizacao,
            ordemServico.RecebimentoSfit, ordemServico.AberturaSfit, ordemServico.DataFiscalizacao,
            ordemServico.PrazoNad, ordemServico.PrazoNco, ordemServico.ElaboracaoAutos, ordemServico.DataFinal,
            ordemServico.Situacao, ordemServico.Observacoes);

        ordemServico.DefinirHashIntegridade(hashService.ComputeSha256(conteudo));
    }

    private static Coordenada? CriarCoordenada(double? latitude, double? longitude) =>
        latitude.HasValue && longitude.HasValue ? new Coordenada(latitude.Value, longitude.Value) : null;

    private static string MensagemDeErro(IEnumerable<FluentValidation.Results.ValidationFailure> erros) =>
        string.Join(" ", erros.Select(e => e.ErrorMessage));
}
