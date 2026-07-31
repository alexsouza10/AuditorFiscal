namespace AuditorFiscal.Domain.Enums;

public enum TipoFiscalizacao
{
    Direta = 0,
    Indireta = 1,
    Mista = 2
}

public static class TipoFiscalizacaoExtensions
{
    public static string Descricao(this TipoFiscalizacao tipo) => tipo switch
    {
        TipoFiscalizacao.Direta => "Direta",
        TipoFiscalizacao.Indireta => "Indireta",
        TipoFiscalizacao.Mista => "Mista",
        _ => tipo.ToString()
    };
}
