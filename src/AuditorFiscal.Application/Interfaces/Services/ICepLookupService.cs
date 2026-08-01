namespace AuditorFiscal.Application.Interfaces.Services;

public sealed record EnderecoCepDto(string Logradouro, string Bairro, string Cidade, string Uf);

/// <summary>
/// Busca de endereço por CEP. É um atalho opcional para preencher o formulário mais rápido —
/// se a máquina estiver offline ou o serviço externo falhar, a busca simplesmente retorna nulo
/// e o auditor preenche o endereço manualmente, sem travar o app.
/// </summary>
public interface ICepLookupService
{
    Task<EnderecoCepDto?> BuscarAsync(string cep, CancellationToken ct = default);
}
