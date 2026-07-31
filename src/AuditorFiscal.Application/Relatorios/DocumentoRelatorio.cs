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
            new(EstiloLinha.Subtitulo, "Identificação"),
            new(EstiloLinha.Campo, "Empresa", os.Empresa),
            new(EstiloLinha.Campo, "CNPJ", os.Cnpj.Formatado()),
            new(EstiloLinha.Campo, "Endereço", os.Endereco),
            new(EstiloLinha.Campo, "Cidade", os.Cidade),
            new(EstiloLinha.Campo, "Responsável", os.Responsavel),
            new(EstiloLinha.Separador, string.Empty),
            new(EstiloLinha.Subtitulo, "Auditoria"),
            new(EstiloLinha.Campo, "Data", os.Data.ToString("dd/MM/yyyy")),
            new(EstiloLinha.Campo, "Hora", os.Hora.ToString("HH\\:mm")),
            new(EstiloLinha.Campo, "Situação", os.Situacao.Descricao()),
            new(EstiloLinha.Campo, "Tipo", os.TipoAuditoria?.Nome ?? "-")
        };

        if (os.Coordenada is not null)
            linhas.Add(new LinhaRelatorio(EstiloLinha.Campo, "Coordenadas",
                $"{os.Coordenada.Latitude:F6}, {os.Coordenada.Longitude:F6}"));

        if (os.Tags.Count > 0)
            linhas.Add(new LinhaRelatorio(EstiloLinha.Campo, "Tags", string.Join(", ", os.Tags.Select(t => t.Nome))));

        if (!string.IsNullOrWhiteSpace(os.Observacoes))
        {
            linhas.Add(new LinhaRelatorio(EstiloLinha.Separador, string.Empty));
            linhas.Add(new LinhaRelatorio(EstiloLinha.Subtitulo, "Observações"));
            linhas.Add(new LinhaRelatorio(EstiloLinha.Texto, os.Observacoes));
        }

        linhas.Add(new LinhaRelatorio(EstiloLinha.Separador, string.Empty));
        linhas.Add(new LinhaRelatorio(EstiloLinha.Subtitulo, "Anexos"));
        linhas.Add(new LinhaRelatorio(EstiloLinha.Campo, "Fotos", os.Fotos.Count.ToString()));
        linhas.Add(new LinhaRelatorio(EstiloLinha.Campo, "Documentos", os.Anexos.Count.ToString()));

        if (os.Timeline.Count > 0)
        {
            linhas.Add(new LinhaRelatorio(EstiloLinha.Separador, string.Empty));
            linhas.Add(new LinhaRelatorio(EstiloLinha.Subtitulo, "Histórico"));
            foreach (var evento in os.Timeline.OrderBy(t => t.OcorridoEm))
                linhas.Add(new LinhaRelatorio(EstiloLinha.Campo,
                    evento.OcorridoEm.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), evento.Descricao));
        }

        if (!string.IsNullOrWhiteSpace(os.HashIntegridade))
        {
            linhas.Add(new LinhaRelatorio(EstiloLinha.Separador, string.Empty));
            linhas.Add(new LinhaRelatorio(EstiloLinha.Texto, $"Hash de integridade (SHA-256): {os.HashIntegridade}"));
        }

        return new DocumentoRelatorio($"OS {os.Numero}", linhas);
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

        linhas.Add(new LinhaRelatorio(EstiloLinha.Separador, string.Empty));
        linhas.Add(new LinhaRelatorio(EstiloLinha.Subtitulo, "Ordens de serviço"));

        foreach (var os in ordens.OrderBy(o => o.Data).ThenBy(o => o.Hora))
            linhas.Add(new LinhaRelatorio(EstiloLinha.Campo,
                $"{os.Data:dd/MM/yyyy} {os.Hora:HH\\:mm}  {os.Numero}",
                $"{os.Empresa} — {os.Cidade} — {os.Situacao.Descricao()}"));

        return new DocumentoRelatorio(titulo, linhas);
    }
}
