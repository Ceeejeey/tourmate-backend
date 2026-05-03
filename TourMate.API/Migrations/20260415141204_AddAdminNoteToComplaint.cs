using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourMate.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminNoteToComplaint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Complaints");
        }
    }
}
