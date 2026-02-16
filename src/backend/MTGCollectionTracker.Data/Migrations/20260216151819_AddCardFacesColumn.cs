using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTGCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCardFacesColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Faces column as JSONB type for efficient storage and querying
            migrationBuilder.AddColumn<string>(
                name: "Faces",
                table: "Cards",
                type: "jsonb",
                nullable: true);

            // Add CHECK constraint to ensure Faces is NULL or a JSON array
            migrationBuilder.Sql(@"
                ALTER TABLE ""Cards""
                ADD CONSTRAINT ""CK_Cards_Faces_Array""
                CHECK (""Faces"" IS NULL OR jsonb_typeof(""Faces"") = 'array')
            ");

            // Add GIN index for searching face names within the JSONB array
            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_Cards_Faces""
                ON ""Cards""
                USING GIN (""Faces"" jsonb_path_ops)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Cards_Faces""");
            migrationBuilder.Sql(@"ALTER TABLE ""Cards"" DROP CONSTRAINT IF EXISTS ""CK_Cards_Faces_Array""");

            migrationBuilder.DropColumn(
                name: "Faces",
                table: "Cards");
        }
    }
}
