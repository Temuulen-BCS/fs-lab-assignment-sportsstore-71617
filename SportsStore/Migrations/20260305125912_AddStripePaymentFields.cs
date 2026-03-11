using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsStore.Migrations
{
    /// <inheritdoc />
    public partial class AddStripePaymentFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAtUtc",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PaymentAmount",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentCurrency",
                table: "Orders",
                type: "nvarchar(10)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Orders",
                type: "nvarchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "Orders",
                type: "nvarchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSessionId",
                table: "Orders",
                type: "nvarchar(255)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PaidAtUtc", table: "Orders");
            migrationBuilder.DropColumn(name: "PaymentAmount", table: "Orders");
            migrationBuilder.DropColumn(name: "PaymentCurrency", table: "Orders");
            migrationBuilder.DropColumn(name: "PaymentStatus", table: "Orders");
            migrationBuilder.DropColumn(name: "StripePaymentIntentId", table: "Orders");
            migrationBuilder.DropColumn(name: "StripeSessionId", table: "Orders");
        }
    }
}
