namespace AuditorFiscal.Domain.Enums;

public enum PapelAuditor
{
    Principal = 0,
    Secundario = 1
}

public static class PapelAuditorExtensions
{
    public static string Descricao(this PapelAuditor papel) => papel switch
    {
        PapelAuditor.Principal => "Principal",
        PapelAuditor.Secundario => "Secundário",
        _ => papel.ToString()
    };
}
