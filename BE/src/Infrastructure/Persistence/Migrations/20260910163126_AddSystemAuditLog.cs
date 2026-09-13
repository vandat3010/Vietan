using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "system_audit_logs",
                schema: "scada",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    endpoint = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    http_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    http_status_code = table.Column<int>(type: "integer", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    additional_data = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_audit_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_system_audit_logs_action",
                schema: "scada",
                table: "system_audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_system_audit_logs_correlation_id",
                schema: "scada",
                table: "system_audit_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_audit_logs_created_at",
                schema: "scada",
                table: "system_audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_system_audit_logs_event_type",
                schema: "scada",
                table: "system_audit_logs",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "IX_system_audit_logs_status",
                schema: "scada",
                table: "system_audit_logs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_system_audit_logs_user_name",
                schema: "scada",
                table: "system_audit_logs",
                column: "user_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_audit_logs",
                schema: "scada");
        }
    }
}
