using AuditorFiscal.Domain.Exceptions;

namespace AuditorFiscal.Domain.ValueObjects;

public sealed record Coordenada
{
    public double Latitude { get; }
    public double Longitude { get; }

    public Coordenada(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new DomainException("Latitude deve estar entre -90 e 90.");

        if (longitude is < -180 or > 180)
            throw new DomainException("Longitude deve estar entre -180 e 180.");

        Latitude = latitude;
        Longitude = longitude;
    }
}
