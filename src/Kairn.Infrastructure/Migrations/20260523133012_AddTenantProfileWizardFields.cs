using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantProfileWizardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FiscalYearStartMonth",
                table: "TenantProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LegalForm",
                table: "TenantProfiles",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatFilingFrequency",
                table: "TenantProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiscalYearStartMonth",
                table: "TenantProfiles");

            migrationBuilder.DropColumn(
                name: "LegalForm",
                table: "TenantProfiles");

            migrationBuilder.DropColumn(
                name: "VatFilingFrequency",
                table: "TenantProfiles");
        }
    }
}
