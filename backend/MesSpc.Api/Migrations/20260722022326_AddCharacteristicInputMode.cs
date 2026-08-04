using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacteristicInputMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DecimalPlaces",
                table: "QualityCharacteristics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InputMode",
                table: "QualityCharacteristics",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "DIRECT");

            migrationBuilder.AddColumn<string>(
                name: "ValueLabel",
                table: "QualityCharacteristics",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "量測值");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DecimalPlaces",
                table: "QualityCharacteristics");

            migrationBuilder.DropColumn(
                name: "InputMode",
                table: "QualityCharacteristics");

            migrationBuilder.DropColumn(
                name: "ValueLabel",
                table: "QualityCharacteristics");
        }
    }
}
