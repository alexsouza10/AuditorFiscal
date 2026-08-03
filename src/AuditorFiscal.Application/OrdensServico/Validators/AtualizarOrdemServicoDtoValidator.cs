using AuditorFiscal.Application.OrdensServico.Dtos;
using AuditorFiscal.Domain.ValueObjects;
using FluentValidation;

namespace AuditorFiscal.Application.OrdensServico.Validators;

public class AtualizarOrdemServicoDtoValidator : AbstractValidator<AtualizarOrdemServicoDto>
{
    public AtualizarOrdemServicoDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Numero).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Empresa).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cnpj).NotEmpty().Must(Cnpj.EhValido).WithMessage("CNPJ inválido.");
        RuleFor(x => x.Endereco).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Cidade).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Responsavel).MaximumLength(150);
        RuleFor(x => x.Observacoes).MaximumLength(4000);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x)
            .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
            .WithMessage("Informe latitude e longitude juntas, ou deixe as duas em branco.")
            .OverridePropertyName(nameof(AtualizarOrdemServicoDto.Longitude));

        RuleFor(x => x.AberturaSfit).GreaterThanOrEqualTo(x => x.RecebimentoSfit)
            .WithMessage("Abertura no SFIT não pode ser antes do recebimento.");
        RuleFor(x => x.DataFiscalizacao).GreaterThanOrEqualTo(x => x.AberturaSfit)
            .WithMessage("Data da fiscalização não pode ser antes da abertura no SFIT.");
        RuleFor(x => x.PrazoNad).GreaterThanOrEqualTo(x => x.DataFiscalizacao)
            .WithMessage("Prazo NAD não pode ser antes da fiscalização.");
        RuleFor(x => x.PrazoNco).GreaterThanOrEqualTo(x => x.PrazoNad)
            .WithMessage("Prazo NCO não pode ser antes do prazo NAD.");
        RuleFor(x => x.ElaboracaoAutos).GreaterThanOrEqualTo(x => x.PrazoNco)
            .WithMessage("Elaboração dos autos não pode ser antes do prazo NCO.");
        RuleFor(x => x.DataFinal).GreaterThanOrEqualTo(x => x.ElaboracaoAutos)
            .WithMessage("Data final não pode ser antes da elaboração dos autos.");

        RuleFor(x => x.PrazoNcre).NotNull().When(x => x.TemNcre)
            .WithMessage("Informe o prazo do NCRE.");
        RuleFor(x => x.PrazoNcre).LessThanOrEqualTo(x => x.DataFinal)
            .When(x => x.TemNcre && x.PrazoNcre.HasValue)
            .WithMessage("Prazo do NCRE não pode ser depois da data final.");
    }
}
