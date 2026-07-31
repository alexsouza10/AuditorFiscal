namespace AuditorFiscal.Application.Interfaces.Services;

public interface IHashService
{
    string ComputeSha256(byte[] dados);
    string ComputeSha256(string texto);
}
