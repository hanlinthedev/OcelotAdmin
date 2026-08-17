using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OcelotAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftSourceVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceVersion",
                table: "GatewayDrafts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceVersion",
                table: "GatewayDrafts");
        }
    }
}
