using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDashboardPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDashboardPreferences",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    ShowMonthlyRevenue = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowMonthlyExpenses = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowNetProfit = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowOutstandingAr = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowOutstandingAp = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowCashBalance = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDashboardPreferences", x => x.UserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDashboardPreferences");
        }
    }
}
