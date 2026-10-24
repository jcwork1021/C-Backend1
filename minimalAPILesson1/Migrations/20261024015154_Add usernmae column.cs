using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace minimalAPILesson1.Migrations
{
    /// <inheritdoc />
    public partial class Addusernmaecolumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Players",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Username",
                table: "Players");
        }
    }
}
