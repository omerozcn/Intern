using Microsoft.EntityFrameworkCore.Migrations;
using System.Runtime.InteropServices.Marshalling;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TicketSystem.Migrations
{
     /// <inheritdoc />
     public partial class dbupdate : Migration
     {
          protected override void Up(MigrationBuilder migrationBuilder)
          {
               // Remove the unique index on UserName
               migrationBuilder.DropIndex(
                   name: "IX_AspNetUsers_UserName",
                   table: "AspNetUsers");
          }

          protected override void Down(MigrationBuilder migrationBuilder)
          {
               // Recreate the unique index on UserName if needed
               migrationBuilder.CreateIndex(
                   name: "IX_AspNetUsers_UserName",
                   table: "AspNetUsers",
                   column: "UserName",
                   unique: false);
          }
     }
}
