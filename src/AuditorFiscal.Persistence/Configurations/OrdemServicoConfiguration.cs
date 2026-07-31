using AuditorFiscal.Domain.Entities;
using AuditorFiscal.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditorFiscal.Persistence.Configurations;

public class OrdemServicoConfiguration : IEntityTypeConfiguration<OrdemServico>
{
    public void Configure(EntityTypeBuilder<OrdemServico> builder)
    {
        builder.ToTable("OrdensServico");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Numero).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Empresa).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Endereco).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Cidade).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Responsavel).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Observacoes).HasMaxLength(4000);
        builder.Property(x => x.HashIntegridade).HasMaxLength(64);
        builder.Property(x => x.Situacao).HasConversion<string>().HasMaxLength(30);

        builder.Property(x => x.Cnpj)
            .HasConversion(cnpj => cnpj.Numero, valor => Cnpj.Criar(valor))
            .HasColumnName("Cnpj")
            .HasMaxLength(14)
            .IsRequired();

        builder.Property(x => x.Latitude).HasColumnName("Latitude");
        builder.Property(x => x.Longitude).HasColumnName("Longitude");
        builder.Ignore(x => x.Coordenada);

        builder.Property(x => x.Fiscalizacao).HasConversion<string>().HasMaxLength(20);

        builder.HasMany(x => x.Fotos)
            .WithOne()
            .HasForeignKey(f => f.OrdemServicoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Fotos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Anexos)
            .WithOne()
            .HasForeignKey(a => a.OrdemServicoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Anexos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Timeline)
            .WithOne()
            .HasForeignKey(t => t.OrdemServicoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Timeline).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Tags)
            .WithMany(t => t.OrdensServico)
            .UsingEntity(j => j.ToTable("OrdemServicoTags"));
        builder.Navigation(x => x.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.Numero);
        builder.HasIndex(x => x.Empresa);
        builder.HasIndex(x => x.RecebimentoSfit);
        builder.HasIndex(x => x.DataFinal);
    }
}
