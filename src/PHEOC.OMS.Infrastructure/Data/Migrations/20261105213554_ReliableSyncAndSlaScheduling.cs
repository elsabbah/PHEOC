using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHEOC.OMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReliableSyncAndSlaScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastEvaluatedAt",
                table: "EventSlas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EbsSyncStates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectorCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CursorLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CursorVerificationId = table.Column<int>(type: "int", nullable: false),
                    LastSuccessAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EnabledBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EbsSyncStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EbsSyncStates_ConnectorCode",
                table: "EbsSyncStates",
                column: "ConnectorCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EbsSyncStates");

            migrationBuilder.DropColumn(
                name: "LastEvaluatedAt",
                table: "EventSlas");
        }
    }
}
