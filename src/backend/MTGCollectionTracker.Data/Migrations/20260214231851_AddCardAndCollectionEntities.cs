using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTGCollectionTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCardAndCollectionEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScryfallId = table.Column<Guid>(type: "uuid", nullable: false),
                    OracleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SetCode = table.Column<string>(type: "text", nullable: false),
                    CollectorNumber = table.Column<string>(type: "text", nullable: false),
                    Rarity = table.Column<string>(type: "text", nullable: false),
                    ArenaId = table.Column<int>(type: "integer", nullable: true),
                    MtgoId = table.Column<int>(type: "integer", nullable: true),
                    ManaCost = table.Column<string>(type: "text", nullable: true),
                    Cmc = table.Column<decimal>(type: "numeric", nullable: false),
                    TypeLine = table.Column<string>(type: "text", nullable: false),
                    OracleText = table.Column<string>(type: "text", nullable: true),
                    Power = table.Column<string>(type: "text", nullable: true),
                    Toughness = table.Column<string>(type: "text", nullable: true),
                    Colors = table.Column<string>(type: "text", nullable: true),
                    ImageUris = table.Column<string>(type: "text", nullable: true),
                    Legalities = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollectionEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    AcquiredDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionEntries_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ArenaId",
                table: "Cards",
                column: "ArenaId",
                filter: "\"ArenaId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_MtgoId",
                table: "Cards",
                column: "MtgoId",
                filter: "\"MtgoId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_Name",
                table: "Cards",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_OracleId",
                table: "Cards",
                column: "OracleId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ScryfallId",
                table: "Cards",
                column: "ScryfallId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cards_SetCode_CollectorNumber",
                table: "Cards",
                columns: new[] { "SetCode", "CollectorNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionEntries_CardId",
                table: "CollectionEntries",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionEntries_Platform",
                table: "CollectionEntries",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionEntries_UserId",
                table: "CollectionEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionEntries_UserId_CardId_Platform",
                table: "CollectionEntries",
                columns: new[] { "UserId", "CardId", "Platform" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionEntries");

            migrationBuilder.DropTable(
                name: "Cards");
        }
    }
}
