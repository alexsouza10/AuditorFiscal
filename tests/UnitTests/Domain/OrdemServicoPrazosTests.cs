using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.Enums;
using AuditorFiscal.Domain.ValueObjects;
using AwesomeAssertions;

namespace UnitTests.Domain;

public class OrdemServicoPrazosTests
{
    private static OrdemServico CriarAtiva(
        DateOnly prazoNad, DateOnly prazoNco, DateOnly dataFinal, DateTimeOffset? momento = null) => new(
        numero: "OS-2026-0002",
        empresa: "Empresa Prazos Ltda",
        cnpj: Cnpj.Criar("11.444.777/0001-61"),
        endereco: "Rua Exemplo, 123",
        cidade: "São Paulo",
        responsavel: "João da Silva",
        fiscalizacao: TipoFiscalizacao.Direta,
        recebimentoSfit: prazoNad.AddDays(-10),
        aberturaSfit: prazoNad.AddDays(-9),
        dataFiscalizacao: prazoNad.AddDays(-5),
        prazoNad: prazoNad,
        prazoNco: prazoNco,
        elaboracaoAutos: dataFinal.AddDays(-3),
        dataFinal: dataFinal,
        momento: momento ?? DateTimeOffset.UtcNow);

    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public void EstaAtrasada_QuandoAtivaEDataFinalNoPassado_DeveSerTrue()
    {
        var ordemServico = CriarAtiva(Hoje.AddDays(-20), Hoje.AddDays(-10), Hoje.AddDays(-1));

        ordemServico.EstaAtrasada(Hoje).Should().BeTrue();
        ordemServico.DiasAtraso(Hoje).Should().Be(1);
    }

    [Fact]
    public void EstaAtrasada_QuandoConcluida_DeveSerFalseMesmoComDataFinalNoPassado()
    {
        var ordemServico = CriarAtiva(Hoje.AddDays(-20), Hoje.AddDays(-10), Hoje.AddDays(-1));
        ordemServico.AlterarSituacao(SituacaoOS.Concluida, DateTimeOffset.UtcNow);

        ordemServico.EstaAtrasada(Hoje).Should().BeFalse();
        ordemServico.DiasAtraso(Hoje).Should().Be(0);
    }

    [Fact]
    public void EstaAtrasada_QuandoDataFinalNoFuturo_DeveSerFalse()
    {
        var ordemServico = CriarAtiva(Hoje.AddDays(3), Hoje.AddDays(7), Hoje.AddDays(10));

        ordemServico.EstaAtrasada(Hoje).Should().BeFalse();
    }

    [Fact]
    public void DiasSemMovimentacao_DeveContarDiasDesdeAtualizadoEm()
    {
        var momentoCriacao = DateTimeOffset.UtcNow.AddDays(-20);
        var ordemServico = CriarAtiva(Hoje.AddDays(10), Hoje.AddDays(15), Hoje.AddDays(20), momentoCriacao);

        ordemServico.DiasSemMovimentacao(Hoje).Should().Be(20);
    }

    [Fact]
    public void DiasSemMovimentacao_QuandoConcluida_DeveSerZero()
    {
        var momentoCriacao = DateTimeOffset.UtcNow.AddDays(-20);
        var ordemServico = CriarAtiva(Hoje.AddDays(10), Hoje.AddDays(15), Hoje.AddDays(20), momentoCriacao);
        ordemServico.AlterarSituacao(SituacaoOS.Concluida, DateTimeOffset.UtcNow);

        ordemServico.DiasSemMovimentacao(Hoje).Should().Be(0);
    }

    [Fact]
    public void PrazosProximos_DeveIncluirApenasCheckpointsDentroDaJanela()
    {
        var ordemServico = CriarAtiva(Hoje.AddDays(5), Hoje.AddDays(30), Hoje.AddDays(60));

        var proximos = ordemServico.PrazosProximos(Hoje, diasJanela: 7);

        proximos.Should().ContainSingle(p => p.Etapa == "Prazo NAD");
    }

    [Fact]
    public void PrazosProximos_QuandoInativa_DeveSerVazio()
    {
        var ordemServico = CriarAtiva(Hoje.AddDays(1), Hoje.AddDays(2), Hoje.AddDays(3));
        ordemServico.AlterarSituacao(SituacaoOS.Cancelada, DateTimeOffset.UtcNow);

        ordemServico.PrazosProximos(Hoje, diasJanela: 7).Should().BeEmpty();
    }

    [Fact]
    public void DiasProximoPrazo_DeveRetornarDiasAteOPrimeiroCheckpointFuturo()
    {
        var ordemServico = CriarAtiva(Hoje.AddDays(-2), Hoje.AddDays(4), Hoje.AddDays(20));

        ordemServico.DiasProximoPrazo(Hoje).Should().Be(4);
    }

    [Fact]
    public void DiasProximoPrazo_QuandoTodosOsCheckpointsJaPassaram_DeveSerNegativoUsandoDataFinal()
    {
        var ordemServico = CriarAtiva(Hoje.AddDays(-20), Hoje.AddDays(-10), Hoje.AddDays(-3));

        ordemServico.DiasProximoPrazo(Hoje).Should().Be(-3);
    }
}
