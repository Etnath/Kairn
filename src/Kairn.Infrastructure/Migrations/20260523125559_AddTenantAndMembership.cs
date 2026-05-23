using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAndMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new nullable column first, copy existing data, then drop the old one
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveTenantId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"AspNetUsers\" SET \"ActiveTenantId\" = \"TenantId\"");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantMemberships",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMemberships", x => new { x.TenantId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TenantMemberships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantMemberships");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropColumn(
                name: "ActiveTenantId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
