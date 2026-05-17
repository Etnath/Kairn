using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                table: "JournalEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RecurringEntryId",
                table: "JournalEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecurringEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EntryDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastPostedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    NextDueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecurringJobLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecurringEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EntryName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    PostedReference = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringJobLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecurringEntryLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecurringEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Debit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Credit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Memo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringEntryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringEntryLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringEntryLines_RecurringEntries_RecurringEntryId",
                        column: x => x.RecurringEntryId,
                        principalTable: "RecurringEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_RecurringEntryId",
                table: "JournalEntries",
                column: "RecurringEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringEntries_TenantId_IsActive_NextDueDate",
                table: "RecurringEntries",
                columns: new[] { "TenantId", "IsActive", "NextDueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringEntryLines_AccountId",
                table: "RecurringEntryLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringEntryLines_RecurringEntryId",
                table: "RecurringEntryLines",
                column: "RecurringEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJobLogs_TenantId_AttemptedAt",
                table: "RecurringJobLogs",
                columns: new[] { "TenantId", "AttemptedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_RecurringEntries_RecurringEntryId",
                table: "JournalEntries",
                column: "RecurringEntryId",
                principalTable: "RecurringEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_RecurringEntries_RecurringEntryId",
                table: "JournalEntries");

            migrationBuilder.DropTable(
                name: "RecurringEntryLines");

            migrationBuilder.DropTable(
                name: "RecurringJobLogs");

            migrationBuilder.DropTable(
                name: "RecurringEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_RecurringEntryId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "IsRecurring",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "RecurringEntryId",
                table: "JournalEntries");
        }
    }
}
