using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditorFiscal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarNcre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "PrazoNcre",
                table: "OrdensServico",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TemNcre",
                table: "OrdensServico",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrazoNcre",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "TemNcre",
                table: "OrdensServico");
        }
    }
}
