using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditorFiscal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPeriodoNcre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PrazoNcre",
                table: "OrdensServico",
                newName: "NcreInicio");

            migrationBuilder.AddColumn<DateOnly>(
                name: "NcreFim",
                table: "OrdensServico",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NcreFim",
                table: "OrdensServico");

            migrationBuilder.RenameColumn(
                name: "NcreInicio",
                table: "OrdensServico",
                newName: "PrazoNcre");
        }
    }
}
