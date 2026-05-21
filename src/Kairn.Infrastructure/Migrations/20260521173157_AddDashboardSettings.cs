using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantDashboardSettings",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CashAlertThreshold = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDashboardSettings", x => x.TenantId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_DisposalJournalEntryId",
                table: "FixedAssets",
                column: "DisposalJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_AccountId",
                table: "BudgetLines",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantDashboardSettings");

            migrationBuilder.DropIndex(
                name: "IX_FixedAssets_DisposalJournalEntryId",
                table: "FixedAssets");

            migrationBuilder.DropIndex(
                name: "IX_BudgetLines_AccountId",
                table: "BudgetLines");
        }
    }
}
