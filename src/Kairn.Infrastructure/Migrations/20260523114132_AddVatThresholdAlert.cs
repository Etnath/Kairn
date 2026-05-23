using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVatThresholdAlert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VatThresholdAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<string>(type: "TEXT", nullable: false),
                    YtdRevenue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Threshold = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    IsDismissed = table.Column<bool>(type: "INTEGER", nullable: false),
                    DismissedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DismissedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatThresholdAlerts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VatThresholdAlerts_TenantId_Year_Level",
                table: "VatThresholdAlerts",
                columns: new[] { "TenantId", "Year", "Level" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VatThresholdAlerts");
        }
    }
}
