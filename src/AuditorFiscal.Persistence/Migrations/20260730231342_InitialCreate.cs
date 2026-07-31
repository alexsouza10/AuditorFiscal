using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuditorFiscal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackupsRealizados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaminhoArquivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    HashSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Automatico = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupsRealizados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogsInternos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OcorridoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Acao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Detalhes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsInternos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Cor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposAuditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposAuditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrdensServico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Empresa = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Cnpj = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    Endereco = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Cidade = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Responsavel = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Data = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    TipoAuditoriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true),
                    Favorito = table.Column<bool>(type: "INTEGER", nullable: false),
                    HashIntegridade = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdensServico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdensServico_TiposAuditoria_TipoAuditoriaId",
                        column: x => x.TipoAuditoriaId,
                        principalTable: "TiposAuditoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anexos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NomeOriginal = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CaminhoArmazenamento = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    HashSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anexos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anexos_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NomeOriginal = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CaminhoArmazenamento = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    HashSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DataCaptura = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fotos_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdemServicoTags",
                columns: table => new
                {
                    OrdensServicoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdemServicoTags", x => new { x.OrdensServicoId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_OrdemServicoTags_OrdensServico_OrdensServicoId",
                        column: x => x.OrdensServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdemServicoTags_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimelineEventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OcorridoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimelineEventos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimelineEventos_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TiposAuditoria",
                columns: new[] { "Id", "Ativo", "AtualizadoEm", "CriadoEm", "Nome" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Auditoria Fiscal" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Diligência Fiscal" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Fiscalização de ICMS" },
                    { new Guid("00000000-0000-0000-0000-000000000004"), true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Fiscalização de ISS" },
                    { new Guid("00000000-0000-0000-0000-000000000005"), true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Auditoria Contábil-Fiscal" },
                    { new Guid("00000000-0000-0000-0000-000000000006"), true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Plantão Fiscal" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anexos_OrdemServicoId",
                table: "Anexos",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_Fotos_OrdemServicoId",
                table: "Fotos",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsInternos_OcorridoEm",
                table: "LogsInternos",
                column: "OcorridoEm");

            migrationBuilder.CreateIndex(
                name: "IX_OrdemServicoTags_TagsId",
                table: "OrdemServicoTags",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_Data",
                table: "OrdensServico",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_Empresa",
                table: "OrdensServico",
                column: "Empresa");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_Numero",
                table: "OrdensServico",
                column: "Numero");

            migrationBuilder.CreateIndex(
                name: "IX_OrdensServico_TipoAuditoriaId",
                table: "OrdensServico",
                column: "TipoAuditoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Nome",
                table: "Tags",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEventos_OrdemServicoId",
                table: "TimelineEventos",
                column: "OrdemServicoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Anexos");

            migrationBuilder.DropTable(
                name: "BackupsRealizados");

            migrationBuilder.DropTable(
                name: "Fotos");

            migrationBuilder.DropTable(
                name: "LogsInternos");

            migrationBuilder.DropTable(
                name: "OrdemServicoTags");

            migrationBuilder.DropTable(
                name: "TimelineEventos");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "OrdensServico");

            migrationBuilder.DropTable(
                name: "TiposAuditoria");
        }
    }
}
