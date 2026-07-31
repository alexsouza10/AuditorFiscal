using AuditorFiscal.Domain.Exceptions;
using AuditorFiscal.Domain.ValueObjects;
using AwesomeAssertions;

namespace UnitTests.Domain;

public class CnpjTests
{
    [Theory]
    [InlineData("11.444.777/0001-61")]
    [InlineData("11444777000161")]
    public void Criar_ComCnpjValido_DeveRetornarInstancia(string valor)
    {
        var cnpj = Cnpj.Criar(valor);

        cnpj.Numero.Should().Be("11444777000161");
        cnpj.Formatado().Should().Be("11.444.777/0001-61");
    }

    [Theory]
    [InlineData("11.444.777/0001-60")]
    [InlineData("00000000000000")]
    [InlineData("123")]
    [InlineData("")]
    public void Criar_ComCnpjInvalido_DeveLancarExcecao(string valor)
    {
        var acao = () => Cnpj.Criar(valor);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void TentarCriar_ComCnpjInvalido_DeveRetornarFalso()
    {
        var resultado = Cnpj.TentarCriar("00000000000000", out var cnpj);

        resultado.Should().BeFalse();
        cnpj.Should().BeNull();
    }

    [Fact]
    public void EhValido_ComCnpjValido_DeveRetornarTrue()
    {
        Cnpj.EhValido("11.444.777/0001-61").Should().BeTrue();
    }
}
