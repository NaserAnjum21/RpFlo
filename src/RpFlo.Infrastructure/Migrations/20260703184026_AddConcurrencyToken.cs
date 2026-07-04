using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RpFlo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyToken : Migration
    {
        /// <inheritdoc />
        // Adding a rowversion column to a temporal table requires dropping and recreating
        // the history table. SQL Server's SYSTEM_VERSIONING = ON validation rejects manually
        // added columns: varbinary(8) fails due to type mismatch with rowversion/timestamp,
        // and rowversion type can't be used in history tables (it auto-generates values).
        // Only SQL Server's own history table creation handles the rowversion mapping correctly.
        //
        // If history data must be preserved: back up history into a staging table before
        // dropping, then reinsert after SYSTEM_VERSIONING is re-enabled.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE [ProcurementRequests] SET (SYSTEM_VERSIONING = OFF);");

            migrationBuilder.Sql(
                "DROP TABLE [ProcurementRequestsHistory];");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProcurementRequests",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.Sql(
                "ALTER TABLE [ProcurementRequests] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.ProcurementRequestsHistory));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE [ProcurementRequests] SET (SYSTEM_VERSIONING = OFF);");

            migrationBuilder.Sql(
                "DROP TABLE [ProcurementRequestsHistory];");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProcurementRequests");

            migrationBuilder.Sql(
                "ALTER TABLE [ProcurementRequests] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.ProcurementRequestsHistory));");
        }
    }
}
