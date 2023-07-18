using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceApplicationMVC.Migrations
{
    /// <inheritdoc />
    public partial class addrequestresult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reqResult",
                table: "Requests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reqResult",
                table: "Requests");
        }
    }
}
