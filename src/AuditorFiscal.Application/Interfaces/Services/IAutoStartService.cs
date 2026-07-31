namespace AuditorFiscal.Application.Interfaces.Services;

public interface IAutoStartService
{
    bool EstaHabilitado();
    void Habilitar();
    void Desabilitar();
}
