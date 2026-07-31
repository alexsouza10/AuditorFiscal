using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditorFiscal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDataFiscalizacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DataFiscalizacao",
                table: "OrdensServico",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // O.S. cadastradas antes deste campo existir não têm uma data real de fiscalização —
            // usar a abertura no SFIT como estimativa inicial é melhor do que deixar 0001-01-01.
            migrationBuilder.Sql("UPDATE OrdensServico SET DataFiscalizacao = AberturaSfit;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataFiscalizacao",
                table: "OrdensServico");
        }
    }
}
