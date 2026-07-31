namespace AuditorFiscal.Shared;

public static class Guard
{
    public static string NotNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Valor não pode ser vazio.", paramName);

        return value;
    }

    public static T NotNull<T>(T? value, string paramName) where T : class
    {
        if (value is null)
            throw new ArgumentNullException(paramName);

        return value;
    }

    public static double InRange(double value, double min, double max, string paramName)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(paramName, value, $"Valor deve estar entre {min} e {max}.");

        return value;
    }
}
