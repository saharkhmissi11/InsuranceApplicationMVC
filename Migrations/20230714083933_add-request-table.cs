using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceApplicationMVC.Migrations
{
    /// <inheritdoc />
    public partial class addrequesttable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    age = table.Column<int>(type: "int", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    carManufacturer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    carType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    riskAssessment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    riskDescription = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requests", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Requests");
        }
    }
}
