using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AuditorFiscal.Application.Interfaces.Services;

namespace AuditorFiscal.Infrastructure.Enderecos;

public sealed class ViaCepLookupService : ICepLookupService, IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(6) };

    public async Task<EnderecoCepDto?> BuscarAsync(string cep, CancellationToken ct = default)
    {
        var digitos = new string((cep ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digitos.Length != 8)
            return null;

        try
        {
            var resposta = await _http.GetFromJsonAsync<ViaCepResposta>(
                $"https://viacep.com.br/ws/{digitos}/json/", ct);

            if (resposta is null || resposta.Erro)
                return null;

            return new EnderecoCepDto(
                resposta.Logradouro ?? string.Empty,
                resposta.Bairro ?? string.Empty,
                resposta.Localidade ?? string.Empty,
                resposta.Uf ?? string.Empty);
        }
        catch (Exception)
        {
            // Sem internet ou serviço indisponível: a busca é só um atalho, não deve travar o formulário.
            return null;
        }
    }

    public void Dispose() => _http.Dispose();

    private sealed class ViaCepResposta
    {
        public string? Logradouro { get; set; }
        public string? Bairro { get; set; }
        public string? Localidade { get; set; }
        public string? Uf { get; set; }

        [JsonPropertyName("erro")]
        public bool Erro { get; set; }
    }
}
