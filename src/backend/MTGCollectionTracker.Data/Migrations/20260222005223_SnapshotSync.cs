using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTGCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL cannot implicitly cast text → jsonb, so we must supply a USING clause.
            // The generated AlterColumn calls omit this, so we use raw SQL instead.
            // Each statement converts the column and validates that existing data is well-formed JSON.
            migrationBuilder.Sql(@"ALTER TABLE ""Cards"" ALTER COLUMN ""Colors""     TYPE jsonb USING ""Colors""::jsonb;");
            migrationBuilder.Sql(@"ALTER TABLE ""Cards"" ALTER COLUMN ""Faces""      TYPE jsonb USING ""Faces""::jsonb;");
            migrationBuilder.Sql(@"ALTER TABLE ""Cards"" ALTER COLUMN ""Finishes""   TYPE jsonb USING ""Finishes""::jsonb;");
            migrationBuilder.Sql(@"ALTER TABLE ""Cards"" ALTER COLUMN ""ImageUris""  TYPE jsonb USING ""ImageUris""::jsonb;");
            migrationBuilder.Sql(@"ALTER TABLE ""Cards"" ALTER COLUMN ""Legalities"" TYPE jsonb USING ""Legalities""::jsonb;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // jsonb → text is implicit in PostgreSQL, so AlterColumn works fine here.
            migrationBuilder.Sql(@"ALTER TABLE ""Cards"" ALTER COLUMN ""Colors""     TYPE text;");
            migrationBuilder.Sql(@"ALTER TABLE ""Cards"" ALTER COLUMN ""Faces""      TYPE text;");
            migrationBuilder.Sql(@"ALTER TABLE ""Cards"" ALTER COLUMN ""Finishes""   TYPE text;");
            migrationBuilder.Sql(@"ALTER TABLE ""Cards"" ALTER COLUMN ""ImageUris""  TYPE text;");
            migrationBuilder.Sql(@"ALTER TABLE ""Cards"" ALTER COLUMN ""Legalities"" TYPE text;");
        }
    }
}
