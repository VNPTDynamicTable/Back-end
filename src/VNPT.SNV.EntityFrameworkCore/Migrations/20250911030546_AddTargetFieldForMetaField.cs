using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNPT.SNV.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetFieldForMetaField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TargetField",
                table: "SNVMetaField",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetField",
                table: "SNVMetaField");
        }
    }
}
