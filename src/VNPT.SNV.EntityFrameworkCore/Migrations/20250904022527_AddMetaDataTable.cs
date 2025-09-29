using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VNPT.SNV.Migrations
{
    /// <inheritdoc />
    public partial class AddMetaDataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SNVMetaTable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TableNameDB = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    DisplayNameVN = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SNVMetaTable", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SNVMetaField",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FieldNameDB = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    DisplayNameVN = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DataType = table.Column<string>(type: "text", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsUnique = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultValue = table.Column<string>(type: "text", nullable: true),
                    TableId = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SNVMetaField", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SNVMetaField_SNVMetaTable_TableId",
                        column: x => x.TableId,
                        principalTable: "SNVMetaTable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SNVMiddleTable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TableNameDB = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    TargetTable1 = table.Column<int>(type: "integer", nullable: false),
                    TargetTable2 = table.Column<int>(type: "integer", nullable: true),
                    TargetField1 = table.Column<int>(type: "integer", nullable: false),
                    TargetField2 = table.Column<int>(type: "integer", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true)
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
                name: "IX_SNVMetaField_TableId",
                table: "SNVMetaField",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_SNVMetaField_TableId_DisplayNameVN",
                table: "SNVMetaField",
                columns: new[] { "TableId", "DisplayNameVN" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SNVMetaField_TableId_FieldNameDB",
                table: "SNVMetaField",
                columns: new[] { "TableId", "FieldNameDB" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SNVMetaTable_DisplayNameVN",
                table: "SNVMetaTable",
                column: "DisplayNameVN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SNVMetaTable_TableNameDB",
                table: "SNVMetaTable",
                column: "TableNameDB",
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SNVMiddleTable");

            migrationBuilder.DropTable(
                name: "SNVMetaField");

            migrationBuilder.DropTable(
                name: "SNVMetaTable");
        }
    }
}
