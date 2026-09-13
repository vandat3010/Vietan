using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTagScreenMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tag_screen_mapping",
                schema: "scada",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tag_id = table.Column<long>(type: "bigint", nullable: false),
                    screen_type = table.Column<int>(type: "integer", nullable: false),
                    is_realtime = table.Column<bool>(type: "boolean", nullable: false),
                    mapping_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_screen_mapping", x => x.id);
                    table.ForeignKey(
                        name: "FK_tag_screen_mapping_tag_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "scada",
                        principalTable: "tag",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tag_screen_mapping_screen_type",
                schema: "scada",
                table: "tag_screen_mapping",
                column: "screen_type");

            migrationBuilder.CreateIndex(
                name: "IX_tag_screen_mapping_screen_type_is_realtime",
                schema: "scada",
                table: "tag_screen_mapping",
                columns: new[] { "screen_type", "is_realtime" });

            migrationBuilder.CreateIndex(
                name: "IX_tag_screen_mapping_tag_id_screen_type",
                schema: "scada",
                table: "tag_screen_mapping",
                columns: new[] { "tag_id", "screen_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tag_screen_mapping",
                schema: "scada");
        }
    }
}
