using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Kairn.Infrastructure.Persistence;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260519090000_AddBillApprovalWorkflow")]
    public partial class AddBillApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Bills",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantApSettings",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApprovalEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApprovalThreshold = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ApproverRoles = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantApSettings", x => x.TenantId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantApSettings");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Bills");
        }
    }
}
