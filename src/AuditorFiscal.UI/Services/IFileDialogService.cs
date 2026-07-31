namespace AuditorFiscal.UI.Services;

public interface IFileDialogService
{
    Task<string?> SalvarComoAsync(string nomeSugerido, string descricaoTipo, string extensao);
    Task<string?> AbrirArquivoAsync(string descricaoTipo, string extensao);
}
