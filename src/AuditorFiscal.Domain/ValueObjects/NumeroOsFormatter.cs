using System.Globalization;

namespace AuditorFiscal.Domain.ValueObjects;

/// <summary>
/// Número da O.S. no padrão 00000000-9: 8 dígitos sequenciais + 1 dígito verificador
/// (soma dos 8 dígitos módulo 10), só para detectar erro de digitação na hora de buscar.
/// </summary>
public static class NumeroOsFormatter
{
    public static string Formatar(int sequencial)
    {
        var oitoDigitos = (Math.Abs(sequencial) % 100_000_000).ToString("D8", CultureInfo.InvariantCulture);
        return $"{oitoDigitos}-{CalcularDigitoVerificador(oitoDigitos)}";
    }

    public static int ExtrairSequencial(string numero)
    {
        var digitos = new string(numero.Where(char.IsDigit).ToArray());
        return digitos.Length >= 8 && int.TryParse(digitos[..8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor)
            ? valor
            : 0;
    }

    private static int CalcularDigitoVerificador(string oitoDigitos) =>
        oitoDigitos.Sum(c => c - '0') % 10;
}
