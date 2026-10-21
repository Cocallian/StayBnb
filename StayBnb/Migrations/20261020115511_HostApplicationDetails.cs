using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayBnb.Migrations
{
    /// <inheritdoc />
    public partial class HostApplicationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PropertyId",
                table: "HostApplications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "HostApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "HostApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CleaningFee",
                table: "HostApplications",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "HostApplications",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "HostApplications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerNight",
                table: "HostApplications",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFee",
                table: "HostApplications",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "HostApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AmenityHostApplication",
                columns: table => new
                {
                    AmenitiesId = table.Column<int>(type: "int", nullable: false),
                    HostApplicationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmenityHostApplication", x => new { x.AmenitiesId, x.HostApplicationId });
                    table.ForeignKey(
                        name: "FK_AmenityHostApplication_Amenities_AmenitiesId",
                        column: x => x.AmenitiesId,
                        principalTable: "Amenities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AmenityHostApplication_HostApplications_HostApplicationId",
                        column: x => x.HostApplicationId,
                        principalTable: "HostApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AmenityHostApplication_HostApplicationId",
                table: "AmenityHostApplication",
                column: "HostApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmenityHostApplication");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "HostApplications");

            migrationBuilder.DropColumn(
                name: "City",
                table: "HostApplications");

            migrationBuilder.DropColumn(
                name: "CleaningFee",
                table: "HostApplications");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "HostApplications");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "HostApplications");

            migrationBuilder.DropColumn(
                name: "PricePerNight",
                table: "HostApplications");

            migrationBuilder.DropColumn(
                name: "ServiceFee",
                table: "HostApplications");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "HostApplications");

            migrationBuilder.AlterColumn<int>(
                name: "PropertyId",
                table: "HostApplications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
