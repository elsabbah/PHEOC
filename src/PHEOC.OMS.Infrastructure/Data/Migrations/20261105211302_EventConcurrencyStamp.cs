using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHEOC.OMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EventConcurrencyStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Warehouses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Teams",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "SitRepReports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "SampleTransfers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Samples",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "RiskPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "RiskAssessments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Personnel",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "LaboratoryResults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "InventoryTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "InventoryItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "InventoryBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "IncidentDecisions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "IncidentAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "ImprovementActions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "FieldReports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Events",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Deployments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Contacts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "ContactFollowUps",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "ClosureRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "ActivationDecisions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "AARReviews",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "SitRepReports");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "SampleTransfers");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "RiskPolicies");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "RiskAssessments");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Personnel");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "LaboratoryResults");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "InventoryBatches");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "IncidentDecisions");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "IncidentAssignments");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "ImprovementActions");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "FieldReports");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "ContactFollowUps");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "ClosureRequests");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "ActivationDecisions");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "AARReviews");
        }
    }
}
