using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTGCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexForUserPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CollectionEntries_UserId_Platform",
                table: "CollectionEntries",
                columns: new[] { "UserId", "Platform" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CollectionEntries_UserId_Platform",
                table: "CollectionEntries");
        }
    }
}
