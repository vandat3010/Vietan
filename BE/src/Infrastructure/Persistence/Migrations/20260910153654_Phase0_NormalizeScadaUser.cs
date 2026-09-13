using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase0_NormalizeScadaUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "created_by",
                schema: "scada",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "department",
                schema: "scada",
                table: "users",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "scada",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                schema: "scada",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "failed_login_count",
                schema: "scada",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "level",
                schema: "scada",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lockout_until",
                schema: "scada",
                table: "users",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "must_change_password",
                schema: "scada",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_updated_at",
                schema: "scada",
                table: "users",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "position",
                schema: "scada",
                table: "users",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unit",
                schema: "scada",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                schema: "scada",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "scada",
                table: "users");

            migrationBuilder.DropColumn(
                name: "department",
                schema: "scada",
                table: "users");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "scada",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email",
                schema: "scada",
                table: "users");

            migrationBuilder.DropColumn(
                name: "failed_login_count",
                schema: "scada",
                table: "users");

            migrationBuilder.DropColumn(
                name: "level",
                schema: "scada",
                table: "users");

            migrationBuilder.DropColumn(
                name: "lockout_until",
                schema: "scada",
                table: "users");

            migrationBuilder.DropColumn(
                name: "must_change_password",
                schema: "scada",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_updated_at",
                schema: "scada",
                table: "users");

            migrationBuilder.DropColumn(
                name: "position",
                schema: "scada",
                table: "users");

            migrationBuilder.DropColumn(
                name: "unit",
                schema: "scada",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "scada",
                table: "users");
        }
    }
}
