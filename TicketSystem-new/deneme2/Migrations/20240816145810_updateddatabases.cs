using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TicketSystem.Migrations
{
    /// <inheritdoc />
    public partial class updateddatabases : Migration
    {
          protected override void Up(MigrationBuilder migrationBuilder)
          {
               migrationBuilder.DropIndex(
                   name: "IX_AspNetUsers_UserName",
                   table: "AspNetUsers");
          }

          protected override void Down(MigrationBuilder migrationBuilder)
          {
               migrationBuilder.CreateIndex(
                   name: "IX_AspNetUsers_UserName",
                   table: "AspNetUsers",
                   column: "UserName",
                   unique: false);
          }
     }
}
