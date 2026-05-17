using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SystemRate",
                table: "JournalLines",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CurrencyRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrencyPair = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    FetchedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyRates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyRates_CurrencyPair_Date",
                table: "CurrencyRates",
                columns: new[] { "CurrencyPair", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrencyRates");

            migrationBuilder.DropColumn(
                name: "SystemRate",
                table: "JournalLines");
        }
    }
}
