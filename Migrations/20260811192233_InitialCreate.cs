using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OcelotAdmin.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gateways",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ConfigStoreType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gateways", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GatewayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "TEXT", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationHistory_Gateways_GatewayId",
                        column: x => x.GatewayId,
                        principalTable: "Gateways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsulGatewaySettings",
                columns: table => new
                {
                    GatewayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ConfigurationKey = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsulGatewaySettings", x => x.GatewayId);
                    table.ForeignKey(
                        name: "FK_ConsulGatewaySettings_Gateways_GatewayId",
                        column: x => x.GatewayId,
                        principalTable: "Gateways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileGatewaySettings",
                columns: table => new
                {
                    GatewayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConfigurationPath = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileGatewaySettings", x => x.GatewayId);
                    table.ForeignKey(
                        name: "FK_FileGatewaySettings_Gateways_GatewayId",
                        column: x => x.GatewayId,
                        principalTable: "Gateways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GatewayDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GatewayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GatewayDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GatewayDrafts_Gateways_GatewayId",
                        column: x => x.GatewayId,
                        principalTable: "Gateways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationHistory_GatewayId_PublishedAt",
                table: "ConfigurationHistory",
                columns: new[] { "GatewayId", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GatewayDrafts_GatewayId",
                table: "GatewayDrafts",
                column: "GatewayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gateways_Name",
                table: "Gateways",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigurationHistory");

            migrationBuilder.DropTable(
                name: "ConsulGatewaySettings");

            migrationBuilder.DropTable(
                name: "FileGatewaySettings");

            migrationBuilder.DropTable(
                name: "GatewayDrafts");

            migrationBuilder.DropTable(
                name: "Gateways");
        }
    }
}
