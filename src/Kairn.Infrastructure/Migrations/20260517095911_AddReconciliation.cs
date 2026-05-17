using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JournalLines_AccountId",
                table: "JournalLines");

            migrationBuilder.AddColumn<bool>(
                name: "IsReconciled",
                table: "JournalLines",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ReconciliationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    MatchedPairCount = table.Column<int>(type: "INTEGER", nullable: false),
                    StatementFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true),
                    Format = table.Column<int>(type: "INTEGER", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationSessions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankStatementLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsMatched = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatementLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankStatementLines_ReconciliationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ReconciliationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BankLineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JournalLineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MatchedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    MatchedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationMatches_BankStatementLines_BankLineId",
                        column: x => x.BankLineId,
                        principalTable: "BankStatementLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReconciliationMatches_JournalLines_JournalLineId",
                        column: x => x.JournalLineId,
                        principalTable: "JournalLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_AccountId_IsReconciled",
                table: "JournalLines",
                columns: new[] { "AccountId", "IsReconciled" });

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_SessionId",
                table: "BankStatementLines",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_BankLineId",
                table: "ReconciliationMatches",
                column: "BankLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_JournalLineId",
                table: "ReconciliationMatches",
                column: "JournalLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_SessionId_BankLineId",
                table: "ReconciliationMatches",
                columns: new[] { "SessionId", "BankLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationSessions_AccountId",
                table: "ReconciliationSessions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationSessions_TenantId_AccountId_StartDate_EndDate",
                table: "ReconciliationSessions",
                columns: new[] { "TenantId", "AccountId", "StartDate", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReconciliationMatches");

            migrationBuilder.DropTable(
                name: "BankStatementLines");

            migrationBuilder.DropTable(
                name: "ReconciliationSessions");

            migrationBuilder.DropIndex(
                name: "IX_JournalLines_AccountId_IsReconciled",
                table: "JournalLines");

            migrationBuilder.DropColumn(
                name: "IsReconciled",
                table: "JournalLines");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_AccountId",
                table: "JournalLines",
                column: "AccountId");
        }
    }
}
