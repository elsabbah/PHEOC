using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PHEOC.OMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TrainingDataSeparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTraining",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTraining",
                table: "Events");
        }
    }
}
