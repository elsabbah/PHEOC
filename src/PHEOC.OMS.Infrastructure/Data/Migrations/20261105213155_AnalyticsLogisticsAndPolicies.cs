using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHEOC.OMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AnalyticsLogisticsAndPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientRequestId",
                table: "FieldReports",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Exceptional",
                table: "ClosureRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PolicyId",
                table: "ClosureRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClosurePolicies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    RequireCompletedTasks = table.Column<bool>(type: "bit", nullable: false),
                    RequireApprovedReport = table.Column<bool>(type: "bit", nullable: false),
                    RequireResourcesSettled = table.Column<bool>(type: "bit", nullable: false),
                    RequireEpidemiologyReview = table.Column<bool>(type: "bit", nullable: false),
                    RequireFieldReview = table.Column<bool>(type: "bit", nullable: false),
                    AllowExceptional = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClosurePolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EarlyResponseMilestones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: true),
                    NotApplicable = table.Column<bool>(type: "bit", nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RecordedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EarlyResponseMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EarlyResponseMilestones_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KpiDateRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    Emergence = table.Column<DateOnly>(type: "date", nullable: true),
                    Detection = table.Column<DateOnly>(type: "date", nullable: true),
                    Notification = table.Column<DateOnly>(type: "date", nullable: true),
                    Excluded = table.Column<bool>(type: "bit", nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RecordedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    MethodologyVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiDateRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiDateRecords_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplyRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BatchId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyRequests_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyRequests_InventoryBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "InventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyRequests_InventoryItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlateNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    GovernorateId = table.Column<int>(type: "int", nullable: false),
                    Available = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_Governorates_GovernorateId",
                        column: x => x.GovernorateId,
                        principalTable: "Governorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAssignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<long>(type: "bigint", nullable: false),
                    DeploymentId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_Deployments_DeploymentId",
                        column: x => x.DeploymentId,
                        principalTable: "Deployments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldReports_ClientRequestId",
                table: "FieldReports",
                column: "ClientRequestId",
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClosureRequests_PolicyId",
                table: "ClosureRequests",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_EarlyResponseMilestones_EventId_Action_Id",
                table: "EarlyResponseMilestones",
                columns: new[] { "EventId", "Action", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_KpiDateRecords_EventId_Id",
                table: "KpiDateRecords",
                columns: new[] { "EventId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_BatchId",
                table: "SupplyRequests",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_EventId",
                table: "SupplyRequests",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_ItemId",
                table: "SupplyRequests",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_DeploymentId",
                table: "VehicleAssignments",
                column: "DeploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_VehicleId",
                table: "VehicleAssignments",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_GovernorateId",
                table: "Vehicles",
                column: "GovernorateId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_PlateNumber",
                table: "Vehicles",
                column: "PlateNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClosureRequests_ClosurePolicies_PolicyId",
                table: "ClosureRequests",
                column: "PolicyId",
                principalTable: "ClosurePolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClosureRequests_ClosurePolicies_PolicyId",
                table: "ClosureRequests");

            migrationBuilder.DropTable(
                name: "ClosurePolicies");

            migrationBuilder.DropTable(
                name: "EarlyResponseMilestones");

            migrationBuilder.DropTable(
                name: "KpiDateRecords");

            migrationBuilder.DropTable(
                name: "SupplyRequests");

            migrationBuilder.DropTable(
                name: "VehicleAssignments");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_FieldReports_ClientRequestId",
                table: "FieldReports");

            migrationBuilder.DropIndex(
                name: "IX_ClosureRequests_PolicyId",
                table: "ClosureRequests");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "FieldReports");

            migrationBuilder.DropColumn(
                name: "Exceptional",
                table: "ClosureRequests");

            migrationBuilder.DropColumn(
                name: "PolicyId",
                table: "ClosureRequests");
        }
    }
}
