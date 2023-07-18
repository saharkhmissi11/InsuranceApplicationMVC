using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceApplicationMVC.Migrations
{
    /// <inheritdoc />
    public partial class addreqProcessInstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "processInstanceId",
                table: "Requests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "processInstanceId",
                table: "Requests");
        }
    }
}
