using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedByName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    AttachmentPath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    AttachmentFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Debit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Credit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Memo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_JournalEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantId_Date",
                table: "JournalEntries",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantId_Reference",
                table: "JournalEntries",
                columns: new[] { "TenantId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_AccountId",
                table: "JournalLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_EntryId",
                table: "JournalLines",
                column: "EntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JournalLines");

            migrationBuilder.DropTable(
                name: "JournalEntries");
        }
    }
}
