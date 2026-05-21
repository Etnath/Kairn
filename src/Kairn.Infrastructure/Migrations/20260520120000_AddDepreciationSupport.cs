using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Kairn.Infrastructure.Persistence;

#nullable disable

namespace Kairn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260520120000_AddDepreciationSupport")]
    public partial class AddDepreciationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepreciationLogs",
                columns: table => new
                {
                    Id          = table.Column<long>(type: "INTEGER", nullable: false)
                                       .Annotation("Sqlite:Autoincrement", true),
                    TenantId    = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssetId     = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssetName   = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Period      = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsSuccess   = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage    = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    PostedReference = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    AttemptedAt     = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepreciationLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepreciationLogs_TenantId_Period_IsSuccess",
                table: "DepreciationLogs",
                columns: new[] { "TenantId", "Period", "IsSuccess" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DepreciationLogs");
        }
    }
}
