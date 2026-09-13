using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemLicenseAndSessionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "session_id",
                schema: "scada",
                table: "refresh_tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "system_licenses",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaxConcurrentUsers = table.Column<int>(type: "integer", nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    ValidTo = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_licenses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_session_id",
                schema: "scada",
                table: "refresh_tokens",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_licenses_IsEnabled",
                schema: "app",
                table: "system_licenses",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_system_licenses_LicenseKey",
                schema: "app",
                table: "system_licenses",
                column: "LicenseKey");

            migrationBuilder.CreateIndex(
                name: "IX_system_licenses_ValidTo",
                schema: "app",
                table: "system_licenses",
                column: "ValidTo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_licenses",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_session_id",
                schema: "scada",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "session_id",
                schema: "scada",
                table: "refresh_tokens");
        }
    }
}
