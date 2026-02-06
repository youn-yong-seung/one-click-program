using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneClick.Server.Migrations
{
    /// <inheritdoc />
    public partial class StandardizePKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubId",
                table: "Subscriptions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ModuleId",
                table: "Modules",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "Categories",
                newName: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Subscriptions",
                newName: "SubId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Modules",
                newName: "ModuleId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Categories",
                newName: "CategoryId");
        }
    }
}
