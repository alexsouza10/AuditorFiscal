using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;
using AuditorFiscal.Domain.ValueObjects;
using AwesomeAssertions;

namespace UnitTests.Domain;

public class OrdemServicoTests
{
    private static OrdemServico CriarOrdemServico() => new(
        numero: "OS-2026-0001",
        empresa: "Empresa Teste Ltda",
        cnpj: Cnpj.Criar("11.444.777/0001-61"),
        endereco: "Rua Exemplo, 123",
        cidade: "São Paulo",
        responsavel: "João da Silva",
        fiscalizacao: TipoFiscalizacao.Direta,
        recebimentoSfit: new DateOnly(2026, 8, 1),
        aberturaSfit: new DateOnly(2026, 8, 3),
        dataFiscalizacao: new DateOnly(2026, 8, 8),
        prazoNad: new DateOnly(2026, 8, 15),
        prazoNco: new DateOnly(2026, 8, 30),
        elaboracaoAutos: new DateOnly(2026, 9, 10),
        dataFinal: new DateOnly(2026, 9, 20),
        momento: DateTimeOffset.UtcNow);

    [Fact]
    public void Criar_DeveIniciarComoEmAndamentoEComEventoNaTimeline()
    {
        var ordemServico = CriarOrdemServico();

        // Ao chegar com data de recebimento no SFIT já preenchida, a O.S. nasce em
        // andamento — diferente do V1, em que "Agendada" fazia sentido sem uma data ainda.
        ordemServico.Situacao.Should().Be(SituacaoOS.EmAndamento);
        ordemServico.Timeline.Should().ContainSingle();
        ordemServico.Favorito.Should().BeFalse();
    }

    [Fact]
    public void AlterarSituacao_ParaValorDiferente_DeveAtualizarERegistrarNaTimeline()
    {
        var ordemServico = CriarOrdemServico();

        ordemServico.AlterarSituacao(SituacaoOS.Concluida, DateTimeOffset.UtcNow);

        ordemServico.Situacao.Should().Be(SituacaoOS.Concluida);
        ordemServico.Timeline.Should().HaveCount(2);
    }

    [Fact]
    public void AlterarSituacao_ParaMesmoValor_NaoDeveDuplicarNaTimeline()
    {
        var ordemServico = CriarOrdemServico();

        ordemServico.AlterarSituacao(SituacaoOS.EmAndamento, DateTimeOffset.UtcNow);

        ordemServico.Timeline.Should().ContainSingle();
    }

    [Fact]
    public void AlternarFavorito_DeveInverterValorAtual()
    {
        var ordemServico = CriarOrdemServico();

        ordemServico.AlternarFavorito();
        ordemServico.Favorito.Should().BeTrue();

        ordemServico.AlternarFavorito();
        ordemServico.Favorito.Should().BeFalse();
    }

    [Fact]
    public void AdicionarFoto_DeveApareceNaColecao()
    {
        var ordemServico = CriarOrdemServico();
        var foto = new Foto(ordemServico.Id, "foto.jpg", "image/jpeg", "caminho/foto.enc", 1024, "hash", DateTimeOffset.UtcNow);

        ordemServico.AdicionarFoto(foto);

        ordemServico.Fotos.Should().ContainSingle();
    }
}
