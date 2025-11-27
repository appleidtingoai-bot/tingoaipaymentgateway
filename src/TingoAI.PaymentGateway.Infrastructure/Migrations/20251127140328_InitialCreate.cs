using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TingoAI.PaymentGateway.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantTransactionReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GlobalPayTransactionReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CustomerFirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomerLastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaymentStatus = table.Column<string>(type: "text", nullable: false),
                    CheckoutUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AccessCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentChannel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ResponseCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ResponseMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CreatedAt",
                table: "Transactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Currency",
                table: "Transactions",
                column: "Currency");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_GlobalPayTransactionReference",
                table: "Transactions",
                column: "GlobalPayTransactionReference");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_MerchantTransactionReference",
                table: "Transactions",
                column: "MerchantTransactionReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PaymentStatus",
                table: "Transactions",
                column: "PaymentStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions");
        }
    }
}
