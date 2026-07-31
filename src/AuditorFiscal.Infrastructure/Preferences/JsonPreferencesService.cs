using System.Text.Json;
using AuditorFiscal.Application.Interfaces.Services;

namespace AuditorFiscal.Infrastructure.Preferences;

public class JsonPreferencesService : IPreferencesService
{
    private static readonly JsonSerializerOptions Opcoes = new() { WriteIndented = true };

    private readonly string _caminhoArquivo;
    private Preferencias _atual;

    public JsonPreferencesService(string diretorioConfig)
    {
        Directory.CreateDirectory(diretorioConfig);
        _caminhoArquivo = Path.Combine(diretorioConfig, "preferences.json");
        _atual = Carregar();
    }

    public Preferencias Atual => _atual;

    public void Salvar(Preferencias preferencias)
    {
        _atual = preferencias;
        File.WriteAllText(_caminhoArquivo, JsonSerializer.Serialize(preferencias, Opcoes));
    }

    private Preferencias Carregar()
    {
        if (!File.Exists(_caminhoArquivo))
            return new Preferencias();

        try
        {
            return JsonSerializer.Deserialize<Preferencias>(File.ReadAllText(_caminhoArquivo)) ?? new Preferencias();
        }
        catch (JsonException)
        {
            // Arquivo corrompido não deve impedir o app de abrir: volta ao padrão.
            return new Preferencias();
        }
    }
}
