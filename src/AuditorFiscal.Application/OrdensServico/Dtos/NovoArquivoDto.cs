namespace AuditorFiscal.Application.OrdensServico.Dtos;

public enum TipoArquivo
{
    Foto = 0,
    Anexo = 1
}

/// <summary>
/// Arquivo escolhido pelo usuário mas ainda não persistido. Permite anexar fotos e
/// documentos antes de a ordem de serviço existir no banco: o conteúdo fica só em
/// memória e é criptografado no momento em que a OS é salva.
/// </summary>
public sealed record NovoArquivoDto(
    string NomeArquivo,
    string ContentType,
    byte[] Conteudo,
    TipoArquivo Tipo);
