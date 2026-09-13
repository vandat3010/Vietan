using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHistory30s : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS history.history_30s (
                  time    timestamptz       NOT NULL,
                  tag_id  bigint            NOT NULL,
                  value   double precision  NOT NULL,
                  PRIMARY KEY (time, tag_id)
                );

                CREATE INDEX IF NOT EXISTS "IX_history_30s_tag_id_time"
                  ON history.history_30s (tag_id, time);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS history.history_30s;""");
        }
    }
}
