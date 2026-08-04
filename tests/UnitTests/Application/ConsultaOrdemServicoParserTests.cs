using AuditorFiscal.Application.OrdensServico.Busca;
using AuditorFiscal.Domain.Enums;
using AwesomeAssertions;

namespace UnitTests.Application;

public class ConsultaOrdemServicoParserTests
{
    [Fact]
    public void Interpretar_ConsultaVazia_DeveRetornarFiltroVazio()
    {
        var filtro = ConsultaOrdemServicoParser.Interpretar(null);

        filtro.Termo.Should().BeNull();
        filtro.SomenteAtrasadas.Should().BeFalse();
    }

    [Theory]
    [InlineData("atrasada")]
    [InlineData("atrasadas")]
    public void Interpretar_TokenAtrasadas_DeveAtivarFiltro(string token) =>
        ConsultaOrdemServicoParser.Interpretar(token).SomenteAtrasadas.Should().BeTrue();

    [Fact]
    public void Interpretar_TokenFavoritas_DeveAtivarFiltro() =>
        ConsultaOrdemServicoParser.Interpretar("favoritas").SomenteFavoritos.Should().BeTrue();

    [Fact]
    public void Interpretar_TokenSemMovimentacao_DeveAtivarFiltro() =>
        ConsultaOrdemServicoParser.Interpretar("sem-movimentacao").SomenteSemMovimentacao.Should().BeTrue();

    [Fact]
    public void Interpretar_TokenHoje_DeveAtivarVenceHoje() =>
        ConsultaOrdemServicoParser.Interpretar("hoje").SomenteVencemHoje.Should().BeTrue();

    [Fact]
    public void Interpretar_TokenSemana_DeveDefinirPrazoMaximoDeSeteDias() =>
        ConsultaOrdemServicoParser.Interpretar("semana").PrazoMaximoDias.Should().Be(7);

    [Fact]
    public void Interpretar_StatusAndamento_DeveMapearParaEmAndamento() =>
        ConsultaOrdemServicoParser.Interpretar("status:andamento").Situacao.Should().Be(SituacaoOS.EmAndamento);

    [Fact]
    public void Interpretar_StatusConcluida_ComAcento_DeveMapearCorretamente() =>
        ConsultaOrdemServicoParser.Interpretar("status:concluída").Situacao.Should().Be(SituacaoOS.Concluida);

    [Fact]
    public void Interpretar_Empresa_DeveExtrairValor() =>
        ConsultaOrdemServicoParser.Interpretar("empresa:toyota").EmpresaContem.Should().Be("toyota");

    [Fact]
    public void Interpretar_Cidade_DeveExtrairValor() =>
        ConsultaOrdemServicoParser.Interpretar("cidade:recife").CidadeContem.Should().Be("recife");

    [Fact]
    public void Interpretar_Responsavel_ComOuSemAcento_DeveExtrairValor()
    {
        ConsultaOrdemServicoParser.Interpretar("responsavel:alex").ResponsavelContem.Should().Be("alex");
        ConsultaOrdemServicoParser.Interpretar("responsável:alex").ResponsavelContem.Should().Be("alex");
    }

    [Fact]
    public void Interpretar_Tag_DeveExtrairValor() =>
        ConsultaOrdemServicoParser.Interpretar("tag:urgente").TagNome.Should().Be("urgente");

    [Theory]
    [InlineData("prazo<10", null, 9)]
    [InlineData("prazo<=10", null, 10)]
    [InlineData("prazo>10", 11, null)]
    [InlineData("prazo>=10", 10, null)]
    public void Interpretar_TokenPrazo_DeveConverterOperadorEmLimiteInclusive(
        string token, int? minimoEsperado, int? maximoEsperado)
    {
        var filtro = ConsultaOrdemServicoParser.Interpretar(token);

        filtro.PrazoMinimoDias.Should().Be(minimoEsperado);
        filtro.PrazoMaximoDias.Should().Be(maximoEsperado);
    }

    [Fact]
    public void Interpretar_ConsultaComposta_DeveCombinarTokensEDeixarRestoComoTermoLivre()
    {
        var filtro = ConsultaOrdemServicoParser.Interpretar("empresa:toyota prazo<5 atrasadas hospital central");

        filtro.EmpresaContem.Should().Be("toyota");
        filtro.PrazoMaximoDias.Should().Be(4);
        filtro.SomenteAtrasadas.Should().BeTrue();
        filtro.Termo.Should().Be("hospital central");
    }

    [Fact]
    public void Interpretar_PalavrasSoltas_DevemVirarTermoLivre() =>
        ConsultaOrdemServicoParser.Interpretar("posto de saúde").Termo.Should().Be("posto de saúde");
}
