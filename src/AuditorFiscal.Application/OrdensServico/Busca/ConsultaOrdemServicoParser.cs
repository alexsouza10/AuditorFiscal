using System.Text.RegularExpressions;
using AuditorFiscal.Application.OrdensServico.Dtos;
using AuditorFiscal.Domain.Enums;

namespace AuditorFiscal.Application.OrdensServico.Busca;

/// <summary>
/// Interpreta uma pesquisa em texto livre no estilo "empresa:toyota prazo&lt;5 atrasadas" e a
/// converte em um <see cref="FiltroOrdemServicoDto"/>. Tokens reconhecidos viram filtros
/// estruturados; o restante do texto vira busca livre (<see cref="FiltroOrdemServicoDto.Termo"/>).
/// </summary>
public static partial class ConsultaOrdemServicoParser
{
    public static FiltroOrdemServicoDto Interpretar(string? consulta)
    {
        if (string.IsNullOrWhiteSpace(consulta))
            return new FiltroOrdemServicoDto();

        var termoLivre = new List<string>();
        SituacaoOS? situacao = null;
        var favoritos = false;
        var atrasadas = false;
        var semMovimentacao = false;
        var venceHoje = false;
        int? prazoMaximo = null;
        int? prazoMinimo = null;
        string? empresa = null;
        string? cidade = null;
        string? responsavel = null;
        string? tag = null;

        foreach (var token in consulta.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var tokenMinusculo = token.ToLowerInvariant();

            if (tokenMinusculo is "atrasada" or "atrasadas")
            {
                atrasadas = true;
            }
            else if (tokenMinusculo is "favorita" or "favoritas" or "favorito" or "favoritos")
            {
                favoritos = true;
            }
            else if (tokenMinusculo is "sem-movimentacao" or "sem-movimentação" or "parada" or "paradas")
            {
                semMovimentacao = true;
            }
            else if (tokenMinusculo == "hoje")
            {
                venceHoje = true;
            }
            else if (tokenMinusculo == "semana")
            {
                prazoMaximo = prazoMaximo is null ? 7 : Math.Min(prazoMaximo.Value, 7);
            }
            else if (TentarPrefixo(token, "status:", out var valorStatus))
            {
                situacao = InterpretarSituacao(valorStatus);
            }
            else if (TentarPrefixo(token, "empresa:", out var valorEmpresa))
            {
                empresa = valorEmpresa;
            }
            else if (TentarPrefixo(token, "cidade:", out var valorCidade))
            {
                cidade = valorCidade;
            }
            else if (TentarPrefixo(token, "responsavel:", out var valorResponsavel) ||
                     TentarPrefixo(token, "responsável:", out valorResponsavel))
            {
                responsavel = valorResponsavel;
            }
            else if (TentarPrefixo(token, "tag:", out var valorTag))
            {
                tag = valorTag;
            }
            else if (RegexPrazo().Match(token) is { Success: true } prazoMatch)
            {
                var operador = prazoMatch.Groups[1].Value;
                var dias = int.Parse(prazoMatch.Groups[2].Value);
                if (operador is "<" or "<=")
                    prazoMaximo = operador == "<" ? dias - 1 : dias;
                else
                    prazoMinimo = operador == ">" ? dias + 1 : dias;
            }
            else
            {
                termoLivre.Add(token);
            }
        }

        return new FiltroOrdemServicoDto
        {
            Termo = termoLivre.Count > 0 ? string.Join(' ', termoLivre) : null,
            Situacao = situacao,
            SomenteFavoritos = favoritos,
            SomenteAtrasadas = atrasadas,
            SomenteSemMovimentacao = semMovimentacao,
            SomenteVencemHoje = venceHoje,
            PrazoMaximoDias = prazoMaximo,
            PrazoMinimoDias = prazoMinimo,
            EmpresaContem = empresa,
            CidadeContem = cidade,
            ResponsavelContem = responsavel,
            TagNome = tag
        };
    }

    private static bool TentarPrefixo(string token, string prefixo, out string? valor)
    {
        if (token.Length > prefixo.Length && token.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
        {
            valor = token[prefixo.Length..];
            return true;
        }

        valor = null;
        return false;
    }

    private static SituacaoOS? InterpretarSituacao(string? valor) => valor?.ToLowerInvariant() switch
    {
        "agendada" or "agendadas" => SituacaoOS.Agendada,
        "andamento" => SituacaoOS.EmAndamento,
        "concluida" or "concluída" or "concluidas" or "concluídas" => SituacaoOS.Concluida,
        "adiada" or "adiadas" => SituacaoOS.Adiada,
        "cancelada" or "canceladas" => SituacaoOS.Cancelada,
        _ => null
    };

    [GeneratedRegex(@"^prazo(<=|>=|<|>)(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex RegexPrazo();
}
