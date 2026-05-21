using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Kairn.Infrastructure.Persistence;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260520130000_AddFiscalYearClose")]
    public partial class AddFiscalYearClose : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FiscalYearCloses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FiscalYear = table.Column<int>(type: "INTEGER", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClosedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    ClosedByName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYearCloses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalYearCloses_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYearCloses_JournalEntryId",
                table: "FiscalYearCloses",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYearCloses_TenantId_FiscalYear",
                table: "FiscalYearCloses",
                columns: new[] { "TenantId", "FiscalYear" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FiscalYearCloses");
        }
    }
}
