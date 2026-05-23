using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantProfiles",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LegalName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Siret = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    AddressLine = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BusinessStatus = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityType = table.Column<string>(type: "TEXT", nullable: false),
                    VatThresholdServices = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    VatThresholdCommercial = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    LogoPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantProfiles", x => x.TenantId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantProfiles");
        }
    }
}
