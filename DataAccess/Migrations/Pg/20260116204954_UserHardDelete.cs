using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations.Pg
{
    /// <inheritdoc />
    public partial class UserHardDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseCategories_Users_UserId",
                table: "ExpenseCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_IncomeCategories_Users_UserId",
                table: "IncomeCategories");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategories_Users_UserId",
                table: "ExpenseCategories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IncomeCategories_Users_UserId",
                table: "IncomeCategories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseCategories_Users_UserId",
                table: "ExpenseCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_IncomeCategories_Users_UserId",
                table: "IncomeCategories");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseCategories_Users_UserId",
                table: "ExpenseCategories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_IncomeCategories_Users_UserId",
                table: "IncomeCategories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
