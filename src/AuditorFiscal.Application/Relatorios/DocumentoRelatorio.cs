using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;

namespace AuditorFiscal.Application.Relatorios;

public enum EstiloLinha
{
    Titulo,
    Subtitulo,
    Campo,
    Texto,
    Separador
}

public sealed record LinhaRelatorio(EstiloLinha Estilo, string Texto, string? Valor = null);

/// <summary>
/// Representação neutra de um relatório, sem qualquer dependência de PDF ou de impressão.
/// PDF e impressora são apenas dois renderizadores da mesma estrutura, evitando duplicar
/// a diagramação e garantindo que o papel e o arquivo saiam idênticos.
/// </summary>
public sealed record DocumentoRelatorio(string Titulo, IReadOnlyList<LinhaRelatorio> Linhas);

public static class RelatorioBuilder
{
    public static DocumentoRelatorio DeOrdemServico(OrdemServico os)
    {
        var linhas = new List<LinhaRelatorio>
        {
            new(EstiloLinha.Titulo, $"Ordem de Serviço {os.Numero}"),
            new(EstiloLinha.Texto, $"Emitido em {DateTime.Now:dd/MM/yyyy HH:mm}"),
            new(EstiloLinha.Separador, string.Empty),
            new(EstiloLinha.Subtitulo, "Identificação")
        };

        AdicionarCampo(linhas, "Empresa", os.Empresa);
        AdicionarCampo(linhas, "CNPJ", os.Cnpj.Formatado());
        AdicionarCampo(linhas, "Endereço", os.Endereco);
        AdicionarCampo(linhas, "Cidade", os.Cidade);
        AdicionarCampo(linhas, "Responsável", os.Responsavel);

        linhas.Add(new LinhaRelatorio(EstiloLinha.Separador, string.Empty));
        linhas.Add(new LinhaRelatorio(EstiloLinha.Subtitulo, "Auditoria"));
        AdicionarCampo(linhas, "Situação", os.Situacao.Descricao());
        AdicionarCampo(linhas, "Fiscalização", os.Fiscalizacao.Descricao());

        linhas.Add(new LinhaRelatorio(EstiloLinha.Separador, string.Empty));
        linhas.Add(new LinhaRelatorio(EstiloLinha.Subtitulo, "Fluxo SFIT"));
        AdicionarCampo(linhas, "1. Recebimento SFIT", os.RecebimentoSfit.ToString("dd/MM/yyyy"));
        AdicionarCampo(linhas, "2. Abertura SFIT", os.AberturaSfit.ToString("dd/MM/yyyy"));
        AdicionarCampo(linhas, "3. Fiscalização", os.DataFiscalizacao.ToString("dd/MM/yyyy"));
        AdicionarCampo(linhas, "4. Prazo NAD", os.PrazoNad.ToString("dd/MM/yyyy"));
        AdicionarCampo(linhas, "5. Prazo NCO", os.PrazoNco.ToString("dd/MM/yyyy"));
        AdicionarCampo(linhas, "6. Elaboração dos autos", os.ElaboracaoAutos.ToString("dd/MM/yyyy"));
        AdicionarCampo(linhas, "7. Data final", os.DataFinal.ToString("dd/MM/yyyy"));

        if (os.TemNcre && os.PrazoNcre is not null)
            AdicionarCampo(linhas, "Prazo NCRE", os.PrazoNcre.Value.ToString("dd/MM/yyyy"));

        if (os.Coordenada is not null)
            AdicionarCampo(linhas, "Coordenadas", $"{os.Coordenada.Latitude:F6}, {os.Coordenada.Longitude:F6}");

        if (!string.IsNullOrWhiteSpace(os.Observacoes))
        {
            linhas.Add(new LinhaRelatorio(EstiloLinha.Separador, string.Empty));
            linhas.Add(new LinhaRelatorio(EstiloLinha.Subtitulo, "Observações"));
            linhas.Add(new LinhaRelatorio(EstiloLinha.Texto, os.Observacoes));
        }

        if (os.Fotos.Count > 0 || os.Anexos.Count > 0)
        {
            linhas.Add(new LinhaRelatorio(EstiloLinha.Separador, string.Empty));
            linhas.Add(new LinhaRelatorio(EstiloLinha.Subtitulo, "Anexos"));

            if (os.Fotos.Count > 0)
                AdicionarCampo(linhas, "Fotos", os.Fotos.Count.ToString());

            if (os.Anexos.Count > 0)
                AdicionarCampo(linhas, "Documentos", os.Anexos.Count.ToString());
        }

        if (os.Timeline.Count > 0)
        {
            linhas.Add(new LinhaRelatorio(EstiloLinha.Separador, string.Empty));
            linhas.Add(new LinhaRelatorio(EstiloLinha.Subtitulo, "Histórico de alterações"));
            foreach (var evento in os.Timeline.OrderBy(t => t.OcorridoEm))
                linhas.Add(new LinhaRelatorio(EstiloLinha.Campo,
                    evento.OcorridoEm.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), evento.Descricao));
        }

        return new DocumentoRelatorio($"OS {os.Numero}", linhas);
    }

    /// <summary>Só entra no PDF o que o auditor de fato preencheu — sem linhas em branco.</summary>
    private static void AdicionarCampo(List<LinhaRelatorio> linhas, string rotulo, string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor))
            linhas.Add(new LinhaRelatorio(EstiloLinha.Campo, rotulo, valor));
    }

    public static DocumentoRelatorio DeLista(string titulo, IReadOnlyList<OrdemServico> ordens)
    {
        var linhas = new List<LinhaRelatorio>
        {
            new(EstiloLinha.Titulo, titulo),
            new(EstiloLinha.Texto, $"Emitido em {DateTime.Now:dd/MM/yyyy HH:mm} — {ordens.Count} ordem(ns) de serviço"),
            new(EstiloLinha.Separador, string.Empty)
        };

        foreach (var grupo in ordens.GroupBy(o => o.Situacao).OrderBy(g => g.Key))
            linhas.Add(new LinhaRelatorio(EstiloLinha.Campo, grupo.Key.Descricao(), grupo.Count().ToString()));

        // Cada O.S. entra com seus próprios campos (rótulo/valor curtos) em vez de uma única
        // linha resumida — um rótulo com data+número já ultrapassava a largura reservada e
        // sobrepunha o texto do valor desenhado ao lado.
        foreach (var os in ordens.OrderBy(o => o.RecebimentoSfit))
        {
            linhas.Add(new LinhaRelatorio(EstiloLinha.Separador, string.Empty));
            linhas.Add(new LinhaRelatorio(EstiloLinha.Subtitulo, $"OS {os.Numero} — {os.Empresa}"));

            AdicionarCampo(linhas, "CNPJ", os.Cnpj.Formatado());
            AdicionarCampo(linhas, "Endereço", os.Endereco);
            AdicionarCampo(linhas, "Cidade", os.Cidade);
            AdicionarCampo(linhas, "Responsável", os.Responsavel);
            AdicionarCampo(linhas, "Situação", os.Situacao.Descricao());
            AdicionarCampo(linhas, "Fiscalização", os.Fiscalizacao.Descricao());
            AdicionarCampo(linhas, "1. Recebimento SFIT", os.RecebimentoSfit.ToString("dd/MM/yyyy"));
            AdicionarCampo(linhas, "2. Abertura SFIT", os.AberturaSfit.ToString("dd/MM/yyyy"));
            AdicionarCampo(linhas, "3. Fiscalização", os.DataFiscalizacao.ToString("dd/MM/yyyy"));
            AdicionarCampo(linhas, "4. Prazo NAD", os.PrazoNad.ToString("dd/MM/yyyy"));
            AdicionarCampo(linhas, "5. Prazo NCO", os.PrazoNco.ToString("dd/MM/yyyy"));
            AdicionarCampo(linhas, "6. Elaboração dos autos", os.ElaboracaoAutos.ToString("dd/MM/yyyy"));
            AdicionarCampo(linhas, "7. Data final", os.DataFinal.ToString("dd/MM/yyyy"));
            AdicionarCampo(linhas, "Observações", os.Observacoes);
        }

        return new DocumentoRelatorio(titulo, linhas);
    }
}
