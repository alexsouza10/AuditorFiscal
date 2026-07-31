using AuditorFiscal.Application.OrdensServico.Dtos;
using AuditorFiscal.Domain.ValueObjects;
using FluentValidation;

namespace AuditorFiscal.Application.OrdensServico.Validators;

public class CriarOrdemServicoDtoValidator : AbstractValidator<CriarOrdemServicoDto>
{
    public CriarOrdemServicoDtoValidator()
    {
        RuleFor(x => x.Numero).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Empresa).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cnpj).NotEmpty().Must(Cnpj.EhValido).WithMessage("CNPJ inválido.");
        RuleFor(x => x.Endereco).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Cidade).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Responsavel).NotEmpty().MaximumLength(150);
        RuleFor(x => x.TipoAuditoriaId).NotEmpty();
        RuleFor(x => x.Observacoes).MaximumLength(4000);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
    }
}
