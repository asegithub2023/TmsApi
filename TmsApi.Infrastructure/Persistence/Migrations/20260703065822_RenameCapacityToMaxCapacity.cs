using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace TmsApi.Migrations
{
    public partial class RenameCapacityToMaxCapacity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Capacity",
                table: "Courses",
                newName: "MaxCapacity");
            migrationBuilder.CreateIndex(
                name: "IX_Courses_Code",
                table: "Courses",
                column: "Code",
                unique: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_Code",
                table: "Courses");
            migrationBuilder.RenameColumn(
                name: "MaxCapacity",
                table: "Courses",
                newName: "Capacity");
        }
    }
}
