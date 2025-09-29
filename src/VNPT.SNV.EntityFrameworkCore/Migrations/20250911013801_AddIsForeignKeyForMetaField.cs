using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VNPT.SNV.Migrations
{
    /// <inheritdoc />
    public partial class AddIsForeignKeyForMetaField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SNVMiddleTable");

            migrationBuilder.AddColumn<bool>(
                name: "IsForeignKey",
                table: "SNVMetaField",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsForeignKey",
                table: "SNVMetaField");

            migrationBuilder.CreateTable(
                name: "SNVMiddleTable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TargetField1 = table.Column<int>(type: "integer", nullable: false),
                    TargetField2 = table.Column<int>(type: "integer", nullable: true),
                    TargetTable1 = table.Column<int>(type: "integer", nullable: false),
                    TargetTable2 = table.Column<int>(type: "integer", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    TableNameDB = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SNVMiddleTable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SNVMiddleTable_SNVMetaField_TargetField1",
                        column: x => x.TargetField1,
                        principalTable: "SNVMetaField",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SNVMiddleTable_SNVMetaField_TargetField2",
                        column: x => x.TargetField2,
                        principalTable: "SNVMetaField",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SNVMiddleTable_SNVMetaTable_TargetTable1",
                        column: x => x.TargetTable1,
                        principalTable: "SNVMetaTable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SNVMiddleTable_SNVMetaTable_TargetTable2",
                        column: x => x.TargetTable2,
                        principalTable: "SNVMetaTable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SNVMiddleTable_TableNameDB",
                table: "SNVMiddleTable",
                column: "TableNameDB",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SNVMiddleTable_TargetField1_TargetField2",
                table: "SNVMiddleTable",
                columns: new[] { "TargetField1", "TargetField2" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SNVMiddleTable_TargetField2",
                table: "SNVMiddleTable",
                column: "TargetField2");

            migrationBuilder.CreateIndex(
                name: "IX_SNVMiddleTable_TargetTable1",
                table: "SNVMiddleTable",
                column: "TargetTable1");

            migrationBuilder.CreateIndex(
                name: "IX_SNVMiddleTable_TargetTable2",
                table: "SNVMiddleTable",
                column: "TargetTable2");
        }
    }
}
