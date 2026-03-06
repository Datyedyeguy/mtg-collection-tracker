using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTGCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImportJobTotalCopies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalCopies",
                table: "ImportJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalCopies",
                table: "ImportJobs");
        }
    }
}
