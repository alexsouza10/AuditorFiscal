using System.Security.Cryptography;
using System.Text;
using AuditorFiscal.Application.Interfaces.Services;
using AuditorFiscal.Infrastructure.Security;
using AwesomeAssertions;

namespace IntegrationTests.Security;

public class AesGcmEncryptionServiceTests
{
    private sealed class ChaveFixaProvider(byte[] chave) : IMasterKeyProvider
    {
        public byte[] ObterChave() => chave;
    }

    private static AesGcmEncryptionService CriarServico(byte[]? chave = null) =>
        new(new ChaveFixaProvider(chave ?? RandomNumberGenerator.GetBytes(32)));

    [Fact]
    public void EncryptEDecrypt_DevemFazerRoundTripCorretamente()
    {
        var servico = CriarServico();
        var original = Encoding.UTF8.GetBytes("conteúdo confidencial de uma ordem de serviço");

        var criptografado = servico.Encrypt(original);
        var decifrado = servico.Decrypt(criptografado);

        decifrado.Should().BeEquivalentTo(original);
        criptografado.Should().NotBeEquivalentTo(original);
    }

    [Fact]
    public void Decrypt_ComChaveDiferente_DeveFalhar()
    {
        var servicoOriginal = CriarServico();
        var servicoComOutraChave = CriarServico();
        var original = Encoding.UTF8.GetBytes("dados sensíveis");

        var criptografado = servicoOriginal.Encrypt(original);

        var acao = () => servicoComOutraChave.Decrypt(criptografado);

        acao.Should().Throw<AuthenticationTagMismatchException>();
    }

    [Fact]
    public void Decrypt_ComDadosAdulterados_DeveFalhar()
    {
        var servico = CriarServico();
        var original = Encoding.UTF8.GetBytes("dados que não podem ser adulterados");
        var criptografado = servico.Encrypt(original);

        criptografado[^1] ^= 0xFF;

        var acao = () => servico.Decrypt(criptografado);

        acao.Should().Throw<AuthenticationTagMismatchException>();
    }

    [Fact]
    public void Encrypt_MesmoConteudo_DeveGerarSaidasDiferentes()
    {
        var servico = CriarServico();
        var original = Encoding.UTF8.GetBytes("mesma entrada");

        var criptografado1 = servico.Encrypt(original);
        var criptografado2 = servico.Encrypt(original);

        criptografado1.Should().NotBeEquivalentTo(criptografado2, "o nonce aleatório garante que cada criptografia é única");
    }
}
