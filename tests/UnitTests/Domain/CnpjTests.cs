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

    [Theory]
    [InlineData("111.444.777-35")]
    [InlineData("11144477735")]
    public void Criar_ComCpfValido_DeveRetornarInstancia(string valor)
    {
        var cpf = Cnpj.Criar(valor);

        cpf.Numero.Should().Be("11144477735");
        cpf.Formatado().Should().Be("111.444.777-35");
        cpf.EhCpf.Should().BeTrue();
    }

    [Theory]
    [InlineData("111.444.777-34")]
    [InlineData("00000000000")]
    public void Criar_ComCpfInvalido_DeveLancarExcecao(string valor)
    {
        var acao = () => Cnpj.Criar(valor);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void EhValido_ComCpfValido_DeveRetornarTrue()
    {
        Cnpj.EhValido("111.444.777-35").Should().BeTrue();
    }
}
