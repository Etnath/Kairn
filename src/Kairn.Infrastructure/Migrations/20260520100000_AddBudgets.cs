using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Kairn.Infrastructure.Persistence;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260520100000_AddBudgets")]
    public partial class AddBudgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Budgets",
                columns: table => new
                {
                    Id         = table.Column<Guid>(type: "TEXT", nullable: false),
                    FiscalYear = table.Column<int>(type: "INTEGER", nullable: false),
                    Name       = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive   = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId   = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt  = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt  = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BudgetLines",
                columns: table => new
                {
                    Id        = table.Column<Guid>(type: "TEXT", nullable: false),
                    BudgetId  = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Month     = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount    = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetLines_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_FiscalYear",
                table: "Budgets",
                columns: new[] { "TenantId", "FiscalYear" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_BudgetId",
                table: "BudgetLines",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_BudgetId_AccountId_Month",
                table: "BudgetLines",
                columns: new[] { "BudgetId", "AccountId", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BudgetLines");
            migrationBuilder.DropTable(name: "Budgets");
        }
    }
}
