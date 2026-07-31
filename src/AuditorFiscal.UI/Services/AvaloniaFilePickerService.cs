using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace AuditorFiscal.UI.Services;

public class AvaloniaFilePickerService : IFilePickerService
{
    public async Task<IReadOnlyList<ArquivoSelecionado>> SelecionarImagensAsync()
    {
        var arquivos = await ObterTopLevel().StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecionar fotos",
            AllowMultiple = true,
            FileTypeFilter = [FilePickerFileTypes.ImageAll]
        });

        return await LerArquivosAsync(arquivos);
    }

    public async Task<IReadOnlyList<ArquivoSelecionado>> SelecionarArquivosAsync()
    {
        var arquivos = await ObterTopLevel().StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecionar anexos",
            AllowMultiple = true
        });

        return await LerArquivosAsync(arquivos);
    }

    private static async Task<IReadOnlyList<ArquivoSelecionado>> LerArquivosAsync(IReadOnlyList<IStorageFile> arquivos)
    {
        var resultado = new List<ArquivoSelecionado>(arquivos.Count);

        foreach (var arquivo in arquivos)
        {
            await using var origem = await arquivo.OpenReadAsync();
            using var memoria = new MemoryStream();
            await origem.CopyToAsync(memoria);

            resultado.Add(new ArquivoSelecionado(arquivo.Name, ResolverContentType(arquivo.Name), memoria.ToArray()));
        }

        return resultado;
    }

    private static string ResolverContentType(string nomeArquivo) => Path.GetExtension(nomeArquivo).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".txt" => "text/plain",
        _ => "application/octet-stream"
    };

    private static TopLevel ObterTopLevel()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } janela })
            return janela;

        throw new InvalidOperationException("Janela principal não disponível para seleção de arquivos.");
    }
}
