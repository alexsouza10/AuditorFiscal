using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AuditorFiscal.UI.ViewModels.Gantt;

/// <summary>
/// Uma linha (uma O.S.) do cronograma. Tanto a margem/largura da barra quanto a largura
/// de cada segmento interno são calculadas como proporção da MESMA janela de tempo
/// visível — nunca da duração da própria O.S. — para que, ao multiplicar qualquer uma
/// dessas proporções pela largura em pixels da régua de meses, tudo alinhe entre si.
/// </summary>
public partial class LinhaGanttViewModel : ObservableObject
{
    public OrdemServico OrdemServico { get; }

    public Guid Id => OrdemServico.Id;
    public string Numero => OrdemServico.Numero;
    public string Empresa => OrdemServico.Empresa;
    public string SituacaoTexto => OrdemServico.Situacao.Descricao();
    public string CorSituacao => OrdemServico.Situacao.Cor();
    public string FiscalizacaoTexto => OrdemServico.Fiscalizacao.Descricao();

    public bool TemNcre => OrdemServico.TemNcre;

    public string NcreTexto => OrdemServico.TemNcre && OrdemServico.NcreInicio.HasValue && OrdemServico.NcreFim.HasValue
        ? $"NCRE: {OrdemServico.NcreInicio.Value:dd/MM/yyyy} a {OrdemServico.NcreFim.Value:dd/MM/yyyy}"
        : "NCRE: não informado";

    /// <summary>Margem esquerda da barra, em proporção da largura total da janela visível.</summary>
    public double MargemEsquerda { get; }

    /// <summary>Largura total da barra, em proporção da largura total da janela visível.</summary>
    public double LarguraTotal { get; }

    /// <summary>Margem esquerda da barra do NCRE, na mesma escala de MargemEsquerda — acompanha a
    /// barra principal, um pouco abaixo dela, do início ao fim do período do NCRE.</summary>
    public double MargemNcre { get; }

    /// <summary>Largura da barra do NCRE, na mesma escala de LarguraTotal.</summary>
    public double LarguraNcre { get; }

    public IReadOnlyList<SegmentoGanttViewModel> Segmentos { get; }

    [ObservableProperty]
    private bool _selecionada;

    public LinhaGanttViewModel(OrdemServico ordemServico, DateOnly inicioJanela, DateOnly fimJanela)
    {
        OrdemServico = ordemServico;

        var totalDiasJanela = Math.Max(1, fimJanela.DayNumber - inicioJanela.DayNumber);

        var inicioBarra = Maior(ordemServico.RecebimentoSfit, inicioJanela);
        var fimBarra = Menor(ordemServico.DataFinal, fimJanela);

        MargemEsquerda = (inicioBarra.DayNumber - inicioJanela.DayNumber) / (double)totalDiasJanela;
        LarguraTotal = Math.Max(0.004, (fimBarra.DayNumber - inicioBarra.DayNumber) / (double)totalDiasJanela);

        if (ordemServico.TemNcre && ordemServico.NcreInicio.HasValue && ordemServico.NcreFim.HasValue)
        {
            var inicioNcre = Maior(ordemServico.NcreInicio.Value, inicioJanela);
            var fimNcre = Menor(ordemServico.NcreFim.Value, fimJanela);
            MargemNcre = (inicioNcre.DayNumber - inicioJanela.DayNumber) / (double)totalDiasJanela;
            LarguraNcre = Math.Max(0.004, (fimNcre.DayNumber - inicioNcre.DayNumber) / (double)totalDiasJanela);
        }

        // Cada segmento é recortado para a mesma janela visível da barra e medido como
        // proporção da janela inteira — a mesma escala usada por MargemEsquerda/LarguraTotal.
        // Esquema de cores 1-6 do cronograma: 1 cinza, 2 azul, 3 verde, 4 amarelo, 5 laranja, 6 vermelho.
        Segmentos =
        [
            CriarSegmento("#D4D4D8", "Recebimento SFIT", ordemServico.RecebimentoSfit, ordemServico.AberturaSfit, inicioJanela, fimJanela, totalDiasJanela),
            CriarSegmento("#3B82F6", "Abertura SFIT", ordemServico.AberturaSfit, ordemServico.DataFiscalizacao, inicioJanela, fimJanela, totalDiasJanela),
            CriarSegmento("#22C55E", "Fiscalização", ordemServico.DataFiscalizacao, ordemServico.PrazoNad, inicioJanela, fimJanela, totalDiasJanela),
            CriarSegmento("#EAB308", "Prazo NAD", ordemServico.PrazoNad, ordemServico.PrazoNco, inicioJanela, fimJanela, totalDiasJanela),
            CriarSegmento("#F97316", "Prazo NCO", ordemServico.PrazoNco, ordemServico.ElaboracaoAutos, inicioJanela, fimJanela, totalDiasJanela),
            CriarSegmento("#EF4444", "Autos / Final", ordemServico.ElaboracaoAutos, ordemServico.DataFinal, inicioJanela, fimJanela, totalDiasJanela)
        ];
    }

    private static SegmentoGanttViewModel CriarSegmento(
        string cor, string rotulo, DateOnly inicio, DateOnly fim, DateOnly inicioJanela, DateOnly fimJanela, int totalDiasJanela)
    {
        var inicioRecortado = Maior(inicio, inicioJanela);
        var fimRecortado = Menor(fim, fimJanela);
        var dias = Math.Max(0, fimRecortado.DayNumber - inicioRecortado.DayNumber);

        return new SegmentoGanttViewModel(cor, rotulo, dias / (double)totalDiasJanela);
    }

    private static DateOnly Maior(DateOnly a, DateOnly b) => a > b ? a : b;
    private static DateOnly Menor(DateOnly a, DateOnly b) => a < b ? a : b;
}
