using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignErdbNamingAndAlarmLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_enable",
                schema: "scada",
                table: "users",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "display_name",
                schema: "scada",
                table: "users",
                newName: "full_name");

            migrationBuilder.RenameColumn(
                name: "is_enable",
                schema: "scada",
                table: "device",
                newName: "is_active");

            migrationBuilder.AddColumn<string>(
                name: "module",
                schema: "history",
                table: "user_activity_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "role",
                schema: "history",
                table: "user_activity_logs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "history",
                table: "user_activity_logs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // user_id may already exist on live DBs from earlier manual/partial DDL.
            migrationBuilder.Sql("""
                ALTER TABLE history.user_activity_logs
                ADD COLUMN IF NOT EXISTS user_id bigint;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "scada",
                table: "tag",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "device_type_id",
                schema: "scada",
                table: "device",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "event_type_id",
                schema: "history",
                table: "alarm_history",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "tag_event_config_id",
                schema: "history",
                table: "alarm_history",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "trigger_type_id",
                schema: "history",
                table: "alarm_history",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "device_type",
                schema: "scada",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_type",
                schema: "scada",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_enable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trigger_type",
                schema: "scada",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_enable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trigger_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tag_event_config",
                schema: "scada",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tag_id = table.Column<long>(type: "bigint", nullable: false),
                    event_type_id = table.Column<long>(type: "bigint", nullable: false),
                    trigger_type_id = table.Column<long>(type: "bigint", nullable: false),
                    trigger_value = table.Column<double>(type: "double precision", nullable: true),
                    deadband = table.Column<double>(type: "double precision", nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    troubleshooting_guide = table.Column<string>(type: "text", nullable: true),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    is_enable = table.Column<bool>(type: "boolean", nullable: false),
                    function_description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_event_config", x => x.id);
                    table.ForeignKey(
                        name: "FK_tag_event_config_event_type_event_type_id",
                        column: x => x.event_type_id,
                        principalSchema: "scada",
                        principalTable: "event_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tag_event_config_tag_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "scada",
                        principalTable: "tag",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tag_event_config_trigger_type_trigger_type_id",
                        column: x => x.trigger_type_id,
                        principalSchema: "scada",
                        principalTable: "trigger_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_user_activity_logs_user_id"
                ON history.user_activity_logs (user_id);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_device_device_type_id",
                schema: "scada",
                table: "device",
                column: "device_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_device_type_code",
                schema: "scada",
                table: "device_type",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_type_code",
                schema: "scada",
                table: "event_type",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tag_event_config_event_type_id",
                schema: "scada",
                table: "tag_event_config",
                column: "event_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_tag_event_config_tag_id",
                schema: "scada",
                table: "tag_event_config",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_tag_event_config_trigger_type_id",
                schema: "scada",
                table: "tag_event_config",
                column: "trigger_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_trigger_type_code",
                schema: "scada",
                table: "trigger_type",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_device_device_type_device_type_id",
                schema: "scada",
                table: "device",
                column: "device_type_id",
                principalSchema: "scada",
                principalTable: "device_type",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_device_device_type_device_type_id",
                schema: "scada",
                table: "device");

            migrationBuilder.DropTable(
                name: "device_type",
                schema: "scada");

            migrationBuilder.DropTable(
                name: "tag_event_config",
                schema: "scada");

            migrationBuilder.DropTable(
                name: "event_type",
                schema: "scada");

            migrationBuilder.DropTable(
                name: "trigger_type",
                schema: "scada");

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS history."IX_user_activity_logs_user_id";
                """);

            migrationBuilder.DropIndex(
                name: "IX_device_device_type_id",
                schema: "scada",
                table: "device");

            migrationBuilder.DropColumn(
                name: "module",
                schema: "history",
                table: "user_activity_logs");

            migrationBuilder.DropColumn(
                name: "role",
                schema: "history",
                table: "user_activity_logs");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "history",
                table: "user_activity_logs");

            // Do not drop user_id — may have pre-existed on live DBs.

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "scada",
                table: "tag");

            migrationBuilder.DropColumn(
                name: "device_type_id",
                schema: "scada",
                table: "device");

            migrationBuilder.DropColumn(
                name: "event_type_id",
                schema: "history",
                table: "alarm_history");

            migrationBuilder.DropColumn(
                name: "tag_event_config_id",
                schema: "history",
                table: "alarm_history");

            migrationBuilder.DropColumn(
                name: "trigger_type_id",
                schema: "history",
                table: "alarm_history");

            migrationBuilder.RenameColumn(
                name: "is_active",
                schema: "scada",
                table: "users",
                newName: "is_enable");

            migrationBuilder.RenameColumn(
                name: "full_name",
                schema: "scada",
                table: "users",
                newName: "display_name");

            migrationBuilder.RenameColumn(
                name: "is_active",
                schema: "scada",
                table: "device",
                newName: "is_enable");
        }
    }
}
