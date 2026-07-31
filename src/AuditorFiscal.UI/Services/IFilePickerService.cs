namespace AuditorFiscal.UI.Services;

public sealed record ArquivoSelecionado(string NomeArquivo, string ContentType, byte[] Conteudo);

/// <summary>
/// Lê o arquivo escolhido pelo usuário inteiramente em memória — nunca grava uma cópia
/// temporária em disco antes da criptografia (ver AuditorFiscal.Infrastructure.Security).
/// </summary>
public interface IFilePickerService
{
    Task<IReadOnlyList<ArquivoSelecionado>> SelecionarImagensAsync();
    Task<IReadOnlyList<ArquivoSelecionado>> SelecionarArquivosAsync();
}
