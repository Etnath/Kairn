using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Kairn.Infrastructure.Persistence;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260519100000_AddExpenseReports")]
    public partial class AddExpenseReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SubmissionDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    SubmittedByName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PaymentJournalEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseReportLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExpenseReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ExpenseAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReceiptFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ReceiptContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReceiptData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseReportLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseReportLines_ExpenseReports_ExpenseReportId",
                        column: x => x.ExpenseReportId,
                        principalTable: "ExpenseReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpenseReportLines_Accounts_ExpenseAccountId",
                        column: x => x.ExpenseAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_TenantId_SubmissionDate",
                table: "ExpenseReports",
                columns: new[] { "TenantId", "SubmissionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_TenantId_Status",
                table: "ExpenseReports",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReportLines_ExpenseReportId",
                table: "ExpenseReportLines",
                column: "ExpenseReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReportLines_ExpenseAccountId",
                table: "ExpenseReportLines",
                column: "ExpenseAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ExpenseReportLines");
            migrationBuilder.DropTable(name: "ExpenseReports");
        }
    }
}
