using Avalonia.Platform.Storage;

namespace AuditorFiscal.UI.Services;

public class AvaloniaFileDialogService : IFileDialogService
{
    public async Task<string?> SalvarComoAsync(string nomeSugerido, string descricaoTipo, string extensao)
    {
        var arquivo = await JanelaAtual.Obter().StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salvar como",
            SuggestedFileName = nomeSugerido,
            DefaultExtension = extensao,
            FileTypeChoices = [new FilePickerFileType(descricaoTipo) { Patterns = [$"*.{extensao}"] }]
        });

        return arquivo?.TryGetLocalPath();
    }

    public async Task<string?> AbrirArquivoAsync(string descricaoTipo, string extensao)
    {
        var arquivos = await JanelaAtual.Obter().StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecionar arquivo",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(descricaoTipo) { Patterns = [$"*.{extensao}"] }]
        });

        return arquivos.Count > 0 ? arquivos[0].TryGetLocalPath() : null;
    }
}
