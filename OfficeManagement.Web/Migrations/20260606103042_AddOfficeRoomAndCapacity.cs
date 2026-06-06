using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfficeManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficeRoomAndCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Offices",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<string>(
                name: "RoomNumber",
                table: "Offices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE Offices SET RoomNumber = CASE OfficeCode
                    WHEN 'OF-001' THEN '101'
                    WHEN 'OF-002' THEN '201'
                    WHEN 'OF-003' THEN '301'
                    ELSE REPLACE(OfficeCode, 'OF-', 'P')
                END;
                UPDATE Offices SET Capacity = CASE OfficeCode
                    WHEN 'OF-001' THEN 8
                    WHEN 'OF-002' THEN 15
                    WHEN 'OF-003' THEN 25
                    ELSE 10
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Offices_RoomNumber",
                table: "Offices",
                column: "RoomNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Offices_RoomNumber",
                table: "Offices");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Offices");

            migrationBuilder.DropColumn(
                name: "RoomNumber",
                table: "Offices");
        }
    }
}
