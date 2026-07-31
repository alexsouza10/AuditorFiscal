using System.Text.RegularExpressions;
using AuditorFiscal.Domain.Exceptions;

namespace AuditorFiscal.Domain.ValueObjects;

public sealed partial class Cnpj : IEquatable<Cnpj>
{
    private static readonly int[] PesosPrimeiroDigito = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosSegundoDigito = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    public string Numero { get; }

    private Cnpj(string numero)
    {
        Numero = numero;
    }

    public static Cnpj Criar(string valor)
    {
        var digitos = ApenasDigitos(valor);

        if (!EhValido(digitos))
            throw new DomainException($"CNPJ inválido: '{valor}'.");

        return new Cnpj(digitos);
    }

    public static bool TentarCriar(string? valor, out Cnpj? cnpj)
    {
        cnpj = null;
        if (string.IsNullOrWhiteSpace(valor))
            return false;

        var digitos = ApenasDigitos(valor);
        if (!EhValido(digitos))
            return false;

        cnpj = new Cnpj(digitos);
        return true;
    }

    public static bool EhValido(string valor)
    {
        var digitos = ApenasDigitos(valor);

        if (digitos.Length != 14)
            return false;

        if (TodosDigitosIguais(digitos))
            return false;

        var primeiroDigito = CalcularDigitoVerificador(digitos[..12], PesosPrimeiroDigito);
        var segundoDigito = CalcularDigitoVerificador(digitos[..12] + primeiroDigito, PesosSegundoDigito);

        return digitos[12] == primeiroDigito && digitos[13] == segundoDigito;
    }

    private static char CalcularDigitoVerificador(string baseDigitos, int[] pesos)
    {
        var soma = 0;
        for (var i = 0; i < pesos.Length; i++)
            soma += (baseDigitos[i] - '0') * pesos[i];

        var resto = soma % 11;
        var digito = resto < 2 ? 0 : 11 - resto;
        return (char)('0' + digito);
    }

    private static bool TodosDigitosIguais(string digitos) => digitos.Distinct().Count() == 1;

    private static string ApenasDigitos(string valor) => NaoDigitoRegex().Replace(valor, string.Empty);

    public string Formatado() =>
        $"{Numero[..2]}.{Numero[2..5]}.{Numero[5..8]}/{Numero[8..12]}-{Numero[12..14]}";

    public override string ToString() => Formatado();

    public bool Equals(Cnpj? other) => other is not null && Numero == other.Numero;

    public override bool Equals(object? obj) => Equals(obj as Cnpj);

    public override int GetHashCode() => Numero.GetHashCode();

    [GeneratedRegex(@"\D")]
    private static partial Regex NaoDigitoRegex();
}
