using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ChatService.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    json_payload = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_created_at",
                table: "outbox_messages",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_is_processed",
                table: "outbox_messages",
                column: "is_processed");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_is_processed_created_at",
                table: "outbox_messages",
                columns: new[] { "is_processed", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Type",
                table: "outbox_messages",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");
        }
    }
}
