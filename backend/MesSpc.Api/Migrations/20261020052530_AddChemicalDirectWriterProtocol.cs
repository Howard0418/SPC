using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChemicalDirectWriterProtocol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("SqlServer"))
                foreach (var sql in ChemicalDirectWriterSqlV1.Up) migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("SqlServer"))
                foreach (var sql in ChemicalDirectWriterSqlV1.Down) migrationBuilder.Sql(sql);
        }
    }
}
