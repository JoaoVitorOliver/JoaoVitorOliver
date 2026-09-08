using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MegaDescontao.Api.Migrations
{
    /// <inheritdoc />
    public partial class UniqueExternalOfferIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Offers_StoreId_ExternalProductId",
                table: "Offers");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_StoreId_ExternalProductId",
                table: "Offers",
                columns: new[] { "StoreId", "ExternalProductId" },
                unique: true,
                filter: "\"ExternalProductId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Offers_StoreId_ExternalProductId",
                table: "Offers");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_StoreId_ExternalProductId",
                table: "Offers",
                columns: new[] { "StoreId", "ExternalProductId" });
        }
    }
}
