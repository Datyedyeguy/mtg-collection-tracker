using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTGCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFinishesTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add foil and etched quantity tracking to collection entries
            migrationBuilder.AddColumn<int>(
                name: "EtchedQuantity",
                table: "CollectionEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FoilQuantity",
                table: "CollectionEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Add finishes array to cards (what finishes are available for this printing)
            migrationBuilder.AddColumn<string>(
                name: "Finishes",
                table: "Cards",
                type: "jsonb",
                nullable: true);

            // Ensure Finishes is an array or null
            migrationBuilder.Sql(@"
                ALTER TABLE ""Cards""
                ADD CONSTRAINT ""CK_Cards_Finishes_Array""
                CHECK (""Finishes"" IS NULL OR jsonb_typeof(""Finishes"") = 'array')
            ");

            // Add GIN index for searching by finish type
            migrationBuilder.CreateIndex(
                name: "IX_Cards_Finishes",
                table: "Cards",
                column: "Finishes")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cards_Finishes",
                table: "Cards");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Cards""
                DROP CONSTRAINT IF EXISTS ""CK_Cards_Finishes_Array""
            ");

            migrationBuilder.DropColumn(
                name: "EtchedQuantity",
                table: "CollectionEntries");

            migrationBuilder.DropColumn(
                name: "FoilQuantity",
                table: "CollectionEntries");

            migrationBuilder.DropColumn(
                name: "Finishes",
                table: "Cards");
        }
    }
}
