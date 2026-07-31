using AuditorFiscal.Application.OrdensServico.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AuditorFiscal.UI.ViewModels;

/// <summary>
/// Representa uma foto/anexo na tela, seja ele já persistido (criptografado em disco)
/// ou apenas escolhido e ainda em memória, aguardando o primeiro salvamento.
/// </summary>
public partial class ArquivoItemViewModel : ObservableObject
{
    public Guid? IdPersistido { get; }
    public NovoArquivoDto? Pendente { get; }
    public string NomeArquivo { get; }
    public long TamanhoBytes { get; }
    public TipoArquivo Tipo { get; }

    public bool EhPendente => Pendente is not null;

    public string DescricaoTamanho => TamanhoBytes switch
    {
        < 1024 => $"{TamanhoBytes} B",
        < 1024 * 1024 => $"{TamanhoBytes / 1024.0:F0} KB",
        _ => $"{TamanhoBytes / (1024.0 * 1024.0):F1} MB"
    };

    public string Situacao => EhPendente ? "pendente" : "criptografado";

    private ArquivoItemViewModel(Guid? idPersistido, NovoArquivoDto? pendente, string nomeArquivo, long tamanhoBytes, TipoArquivo tipo)
    {
        IdPersistido = idPersistido;
        Pendente = pendente;
        NomeArquivo = nomeArquivo;
        TamanhoBytes = tamanhoBytes;
        Tipo = tipo;
    }

    public static ArquivoItemViewModel DePendente(NovoArquivoDto arquivo) =>
        new(null, arquivo, arquivo.NomeArquivo, arquivo.Conteudo.LongLength, arquivo.Tipo);

    public static ArquivoItemViewModel DePersistido(Guid id, string nomeArquivo, long tamanhoBytes, TipoArquivo tipo) =>
        new(id, null, nomeArquivo, tamanhoBytes, tipo);
}
