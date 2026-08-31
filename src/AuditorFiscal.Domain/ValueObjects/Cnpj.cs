using System.Text.RegularExpressions;
using AuditorFiscal.Domain.Exceptions;

namespace AuditorFiscal.Domain.ValueObjects;

// Apesar do nome, também aceita CPF: o auditor pode fiscalizar tanto pessoa jurídica quanto
// pessoa física, e o campo do formulário é único ("CNPJ/CPF") — dividir em dois objetos de
// valor só duplicaria a lógica de formatação/validação para um caso que se resolve pelo
// tamanho do documento (11 dígitos = CPF, 14 = CNPJ).
public sealed partial class Cnpj : IEquatable<Cnpj>
{
    private static readonly int[] PesosCnpjPrimeiroDigito = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosCnpjSegundoDigito = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosCpfPrimeiroDigito = [10, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosCpfSegundoDigito = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];

    public string Numero { get; }
    public bool EhCpf => Numero.Length == 11;

    private Cnpj(string numero)
    {
        Numero = numero;
    }

    public static Cnpj Criar(string valor)
    {
        var digitos = ApenasDigitos(valor);

        if (!EhValido(digitos))
            throw new DomainException($"CNPJ/CPF inválido: '{valor}'.");

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

        return digitos.Length switch
        {
            11 => EhCpfValido(digitos),
            14 => EhCnpjValido(digitos),
            _ => false,
        };
    }

    private static bool EhCnpjValido(string digitos)
    {
        if (TodosDigitosIguais(digitos))
            return false;

        var primeiroDigito = CalcularDigitoVerificador(digitos[..12], PesosCnpjPrimeiroDigito);
        var segundoDigito = CalcularDigitoVerificador(digitos[..12] + primeiroDigito, PesosCnpjSegundoDigito);

        return digitos[12] == primeiroDigito && digitos[13] == segundoDigito;
    }

    private static bool EhCpfValido(string digitos)
    {
        if (TodosDigitosIguais(digitos))
            return false;

        var primeiroDigito = CalcularDigitoVerificador(digitos[..9], PesosCpfPrimeiroDigito);
        var segundoDigito = CalcularDigitoVerificador(digitos[..9] + primeiroDigito, PesosCpfSegundoDigito);

        return digitos[9] == primeiroDigito && digitos[10] == segundoDigito;
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

    public string Formatado() => EhCpf
        ? $"{Numero[..3]}.{Numero[3..6]}.{Numero[6..9]}-{Numero[9..11]}"
        : $"{Numero[..2]}.{Numero[2..5]}.{Numero[5..8]}/{Numero[8..12]}-{Numero[12..14]}";

    public override string ToString() => Formatado();

    public bool Equals(Cnpj? other) => other is not null && Numero == other.Numero;

    public override bool Equals(object? obj) => Equals(obj as Cnpj);

    public override int GetHashCode() => Numero.GetHashCode();

    [GeneratedRegex(@"\D")]
    private static partial Regex NaoDigitoRegex();
}
