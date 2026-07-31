using System.Security.Cryptography;
using AuditorFiscal.Application.Interfaces.Services;

namespace AuditorFiscal.Infrastructure.Security;

/// <summary>
/// Criptografa arquivos individuais (fotos, anexos, backups) com AES-256-GCM.
/// Formato do contêiner: "AFE1" (4 bytes) + versão (1 byte) + nonce (12 bytes) +
/// tag de autenticação (16 bytes) + texto cifrado.
/// </summary>
public class AesGcmEncryptionService(IMasterKeyProvider chaveProvider) : IEncryptionService
{
    private static readonly byte[] Magic = "AFE1"u8.ToArray();
    private const byte Versao = 1;
    private const int TamanhoNonce = 12;
    private const int TamanhoTag = 16;

    public byte[] Encrypt(byte[] dadosOriginais)
    {
        var chave = chaveProvider.ObterChave();
        var nonce = RandomNumberGenerator.GetBytes(TamanhoNonce);
        var tag = new byte[TamanhoTag];
        var textoCifrado = new byte[dadosOriginais.Length];

        using (var aesGcm = new AesGcm(chave, TamanhoTag))
        {
            aesGcm.Encrypt(nonce, dadosOriginais, textoCifrado, tag);
        }

        using var saida = new MemoryStream(Magic.Length + 1 + TamanhoNonce + TamanhoTag + textoCifrado.Length);
        saida.Write(Magic);
        saida.WriteByte(Versao);
        saida.Write(nonce);
        saida.Write(tag);
        saida.Write(textoCifrado);
        return saida.ToArray();
    }

    public byte[] Decrypt(byte[] dadosCriptografados)
    {
        var tamanhoMinimo = Magic.Length + 1 + TamanhoNonce + TamanhoTag;
        if (dadosCriptografados.Length < tamanhoMinimo)
            throw new InvalidOperationException("Dados criptografados inválidos: tamanho insuficiente.");

        var posicao = 0;

        if (!dadosCriptografados.AsSpan(posicao, Magic.Length).SequenceEqual(Magic))
            throw new InvalidOperationException("Formato de arquivo criptografado não reconhecido.");
        posicao += Magic.Length;

        var versao = dadosCriptografados[posicao];
        if (versao != Versao)
            throw new InvalidOperationException($"Versão de criptografia não suportada: {versao}.");
        posicao += 1;

        var nonce = dadosCriptografados.AsSpan(posicao, TamanhoNonce).ToArray();
        posicao += TamanhoNonce;

        var tag = dadosCriptografados.AsSpan(posicao, TamanhoTag).ToArray();
        posicao += TamanhoTag;

        var textoCifrado = dadosCriptografados.AsSpan(posicao).ToArray();

        var chave = chaveProvider.ObterChave();
        var textoDecifrado = new byte[textoCifrado.Length];

        using var aesGcm = new AesGcm(chave, TamanhoTag);
        aesGcm.Decrypt(nonce, textoCifrado, tag, textoDecifrado);

        return textoDecifrado;
    }
}
