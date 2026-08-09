using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditorFiscal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPapelAuditor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PapelAuditor",
                table: "OrdensServico",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Principal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PapelAuditor",
                table: "OrdensServico");
        }
    }
}
