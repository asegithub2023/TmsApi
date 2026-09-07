using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace TmsApi.Migrations
{
    public partial class AddInstructorIdToCourse : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstructorId",
                table: "Courses",
                type: "text",
                nullable: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Courses");
        }
    }
}
