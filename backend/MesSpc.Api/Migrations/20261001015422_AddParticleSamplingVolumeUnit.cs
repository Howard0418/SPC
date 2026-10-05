using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddParticleSamplingVolumeUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SamplingVolumeUnit",
                table: "ParticleMeasurements",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "Unit of the optional sampling volume.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SamplingVolumeUnit",
                table: "ParticleMeasurements");
        }
    }
}
