using DeSanti.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeSanti.Infrastructure.Data.Configurations;

public class PecaOrdemServicoConfiguration : IEntityTypeConfiguration<PecaOrdemServico>
{
    public void Configure(EntityTypeBuilder<PecaOrdemServico> builder)
    {
        builder.ToTable("PecasOrdemServico");

        builder.HasKey(peca => peca.Id);
        builder.Property(peca => peca.Id).ValueGeneratedOnAdd();

        builder.Property(peca => peca.Nome).HasMaxLength(200).IsRequired();
        builder.Property(peca => peca.Quantidade).IsRequired();
        builder.Property(peca => peca.ValorUnitarioCentavos).IsRequired();
        builder.Property(peca => peca.CustoUnitarioCentavos).IsRequired(false);
    }
}