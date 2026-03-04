using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTGCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintCollectionEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CollectionEntries_UserId_CardId_Platform",
                table: "CollectionEntries");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionEntries_UserId_CardId_Platform",
                table: "CollectionEntries",
                columns: new[] { "UserId", "CardId", "Platform" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CollectionEntries_UserId_CardId_Platform",
                table: "CollectionEntries");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionEntries_UserId_CardId_Platform",
                table: "CollectionEntries",
                columns: new[] { "UserId", "CardId", "Platform" });
        }
    }
}
