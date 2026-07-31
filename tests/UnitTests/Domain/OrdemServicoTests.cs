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
        data: new DateOnly(2026, 8, 1),
        hora: new TimeOnly(9, 0),
        tipoAuditoriaId: Guid.NewGuid(),
        momento: DateTimeOffset.UtcNow);

    [Fact]
    public void Criar_DeveIniciarComoAgendadaEComEventoNaTimeline()
    {
        var ordemServico = CriarOrdemServico();

        ordemServico.Situacao.Should().Be(SituacaoOS.Agendada);
        ordemServico.Timeline.Should().ContainSingle();
        ordemServico.Favorito.Should().BeFalse();
    }

    [Fact]
    public void AlterarSituacao_ParaValorDiferente_DeveAtualizarERegistrarNaTimeline()
    {
        var ordemServico = CriarOrdemServico();

        ordemServico.AlterarSituacao(SituacaoOS.EmAndamento, DateTimeOffset.UtcNow);

        ordemServico.Situacao.Should().Be(SituacaoOS.EmAndamento);
        ordemServico.Timeline.Should().HaveCount(2);
    }

    [Fact]
    public void AlterarSituacao_ParaMesmoValor_NaoDeveDuplicarNaTimeline()
    {
        var ordemServico = CriarOrdemServico();

        ordemServico.AlterarSituacao(SituacaoOS.Agendada, DateTimeOffset.UtcNow);

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
