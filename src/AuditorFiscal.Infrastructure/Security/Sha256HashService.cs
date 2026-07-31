using System.Security.Cryptography;
using System.Text;
using AuditorFiscal.Application.Interfaces.Services;

namespace AuditorFiscal.Infrastructure.Security;

public class Sha256HashService : IHashService
{
    public string ComputeSha256(byte[] dados) => Convert.ToHexString(SHA256.HashData(dados));

    public string ComputeSha256(string texto) => ComputeSha256(Encoding.UTF8.GetBytes(texto));
}
