using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditorFiscal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarBuscaFts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tabela virtual FTS5 para busca livre em Pesquisa Poderosa / Pesquisa Global.
            // "Id UNINDEXED" guarda a chave sem tokenizá-la, só para depois religar o resultado
            // do MATCH às linhas reais de OrdensServico.
            migrationBuilder.Sql("""
                CREATE VIRTUAL TABLE OrdensServicoFts USING fts5(
                    Id UNINDEXED,
                    Numero,
                    Empresa,
                    Cidade,
                    Endereco,
                    Responsavel,
                    Observacoes
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO OrdensServicoFts(Id, Numero, Empresa, Cidade, Endereco, Responsavel, Observacoes)
                SELECT Id, Numero, Empresa, Cidade, Endereco, Responsavel, Observacoes FROM OrdensServico;
                """);

            // Gatilhos mantêm o índice sincronizado automaticamente a cada Insert/Update/Delete
            // em OrdensServico, sem exigir que o código da aplicação lembre de atualizar o FTS.
            migrationBuilder.Sql("""
                CREATE TRIGGER OrdensServicoFts_ai AFTER INSERT ON OrdensServico BEGIN
                    INSERT INTO OrdensServicoFts(Id, Numero, Empresa, Cidade, Endereco, Responsavel, Observacoes)
                    VALUES (new.Id, new.Numero, new.Empresa, new.Cidade, new.Endereco, new.Responsavel, new.Observacoes);
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER OrdensServicoFts_ad AFTER DELETE ON OrdensServico BEGIN
                    DELETE FROM OrdensServicoFts WHERE Id = old.Id;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER OrdensServicoFts_au AFTER UPDATE ON OrdensServico BEGIN
                    DELETE FROM OrdensServicoFts WHERE Id = old.Id;
                    INSERT INTO OrdensServicoFts(Id, Numero, Empresa, Cidade, Endereco, Responsavel, Observacoes)
                    VALUES (new.Id, new.Numero, new.Empresa, new.Cidade, new.Endereco, new.Responsavel, new.Observacoes);
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS OrdensServicoFts_au;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS OrdensServicoFts_ad;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS OrdensServicoFts_ai;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS OrdensServicoFts;");
        }
    }
}
