using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTL_WEBDEV2025.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationEmailToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotificationEmail",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationEmail",
                table: "Orders");
        }
    }
}
