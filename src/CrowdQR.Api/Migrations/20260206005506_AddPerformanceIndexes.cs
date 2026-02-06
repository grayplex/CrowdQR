using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrowdQR.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Vote_RequestID",
                table: "Vote",
                newName: "IX_Vote_RequestId");

            migrationBuilder.RenameIndex(
                name: "IX_Request_UserID",
                table: "Request",
                newName: "IX_Request_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Event_DJUserID",
                table: "Event",
                newName: "IX_Event_DjUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Vote_UserId",
                table: "Vote",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Session_EventId",
                table: "Session",
                column: "EventID");

            migrationBuilder.CreateIndex(
                name: "IX_Request_EventId_Status",
                table: "Request",
                columns: new[] { "EventID", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vote_UserId",
                table: "Vote");

            migrationBuilder.DropIndex(
                name: "IX_Session_EventId",
                table: "Session");

            migrationBuilder.DropIndex(
                name: "IX_Request_EventId_Status",
                table: "Request");

            migrationBuilder.RenameIndex(
                name: "IX_Vote_RequestId",
                table: "Vote",
                newName: "IX_Vote_RequestID");

            migrationBuilder.RenameIndex(
                name: "IX_Request_UserId",
                table: "Request",
                newName: "IX_Request_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Event_DjUserId",
                table: "Event",
                newName: "IX_Event_DJUserID");
        }
    }
}
