using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Kairn.Infrastructure.Persistence;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260520110000_AddFixedAssets")]
    public partial class AddFixedAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FixedAssets",
                columns: table => new
                {
                    Id                               = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name                             = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Category                         = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PurchaseDate                     = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PurchaseValue                    = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ResidualValue                    = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Method                           = table.Column<int>(type: "INTEGER", nullable: false),
                    UsefulLifeYears                  = table.Column<int>(type: "INTEGER", nullable: false),
                    AssetAccountId                   = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccumulatedDepreciationAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccumulatedDepreciation          = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    LastDepreciatedDate              = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    HasDepreciationPostings          = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFullyDepreciated               = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DepreciationExpenseAccountId     = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsActive                         = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisposalDate                     = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DisposalJournalEntryId           = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId                         = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt                        = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt                        = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RowVersion                       = table.Column<uint>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FixedAssets_Accounts_AssetAccountId",
                        column: x => x.AssetAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_Accounts_AccumulatedDepreciationAccountId",
                        column: x => x.AccumulatedDepreciationAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_JournalEntries_DisposalJournalEntryId",
                        column: x => x.DisposalJournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FixedAssets_Accounts_DepreciationExpenseAccountId",
                        column: x => x.DepreciationExpenseAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId",
                table: "FixedAssets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_AssetAccountId",
                table: "FixedAssets",
                column: "AssetAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_AccumulatedDepreciationAccountId",
                table: "FixedAssets",
                column: "AccumulatedDepreciationAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_DepreciationExpenseAccountId",
                table: "FixedAssets",
                column: "DepreciationExpenseAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FixedAssets");
        }
    }
}
