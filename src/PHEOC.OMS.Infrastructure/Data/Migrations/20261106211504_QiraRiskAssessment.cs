using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHEOC.OMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class QiraRiskAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QiraDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DocumentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QiraDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QiraNationalPolicies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    HazardsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QiraNationalPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QiraAssessments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    DefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    PolicyId = table.Column<long>(type: "bigint", nullable: true),
                    PreviousId = table.Column<long>(type: "bigint", nullable: true),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    AssessedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssessorName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Population = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TimeWindow = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Hazard = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NovelHazard = table.Column<bool>(type: "bit", nullable: false),
                    Hospitalizations = table.Column<int>(type: "int", nullable: true),
                    IcuAdmissions = table.Column<int>(type: "int", nullable: true),
                    Risk = table.Column<int>(type: "int", nullable: true),
                    Confidence = table.Column<int>(type: "int", nullable: true),
                    ConfidenceReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Uncertainties = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QiraAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QiraAssessments_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QiraAssessments_QiraAssessments_PreviousId",
                        column: x => x.PreviousId,
                        principalTable: "QiraAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QiraAssessments_QiraDefinitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "QiraDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QiraAssessments_QiraNationalPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "QiraNationalPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QiraActions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Organization = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaskId = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SharedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SharedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QiraActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QiraActions_QiraAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "QiraAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QiraActions_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QiraAnswers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<long>(type: "bigint", nullable: false),
                    Node = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Choice = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AttachmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QiraAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QiraAnswers_EventAttachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "EventAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QiraAnswers_QiraAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "QiraAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QiraHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<long>(type: "bigint", nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QiraHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QiraHistories_QiraAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "QiraAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QiraActions_AssessmentId",
                table: "QiraActions",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_QiraActions_TaskId",
                table: "QiraActions",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_QiraAnswers_AssessmentId_Node",
                table: "QiraAnswers",
                columns: new[] { "AssessmentId", "Node" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QiraAnswers_AttachmentId",
                table: "QiraAnswers",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_QiraAssessments_DefinitionId",
                table: "QiraAssessments",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_QiraAssessments_EventId_Revision",
                table: "QiraAssessments",
                columns: new[] { "EventId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QiraAssessments_PolicyId",
                table: "QiraAssessments",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_QiraAssessments_PreviousId",
                table: "QiraAssessments",
                column: "PreviousId");

            migrationBuilder.CreateIndex(
                name: "IX_QiraDefinitions_Code",
                table: "QiraDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QiraHistories_AssessmentId",
                table: "QiraHistories",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_QiraNationalPolicies_Version",
                table: "QiraNationalPolicies",
                column: "Version",
                unique: true);
            migrationBuilder.Sql("""
                IF DATABASE_PRINCIPAL_ID(N'pheoc_runtime') IS NOT NULL
                BEGIN
                  DENY UPDATE, DELETE ON OBJECT::dbo.QiraDefinitions TO pheoc_runtime;
                  DENY UPDATE, DELETE ON OBJECT::dbo.QiraNationalPolicies TO pheoc_runtime;
                  DENY UPDATE, DELETE ON OBJECT::dbo.QiraHistories TO pheoc_runtime;
                END;
                INSERT INTO dbo.AspNetRoleClaims(RoleId,ClaimType,ClaimValue)
                SELECT r.Id, N'permission', p.Value
                FROM dbo.AspNetRoles r
                CROSS JOIN (VALUES(N'QIRA.Review'),(N'QIRA.Reassess')) p(Value)
                WHERE r.NormalizedName=N'SYSTEM ADMINISTRATOR'
                AND NOT EXISTS(SELECT 1 FROM dbo.AspNetRoleClaims c WHERE c.RoleId=r.Id AND c.ClaimType=N'permission' AND c.ClaimValue=p.Value);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QiraActions");

            migrationBuilder.DropTable(
                name: "QiraAnswers");

            migrationBuilder.DropTable(
                name: "QiraHistories");

            migrationBuilder.DropTable(
                name: "QiraAssessments");

            migrationBuilder.DropTable(
                name: "QiraDefinitions");

            migrationBuilder.DropTable(
                name: "QiraNationalPolicies");
        }
    }
}
