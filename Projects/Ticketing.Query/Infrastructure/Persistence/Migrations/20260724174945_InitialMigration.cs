using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Tiketing.Query.Infrastructure.Persintence.Mogrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employess",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    address_street = table.Column<string>(type: "text", nullable: true),
                    address_city = table.Column<string>(type: "text", nullable: true),
                    address_country = table.Column<string>(type: "text", nullable: true),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modificate_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modifie_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employess", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tickects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    ticket_type = table.Column<int>(type: "integer", nullable: true),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modificate_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modifie_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tickects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ticket_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ticket_employess",
                columns: table => new
                {
                    ticked_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employed_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_employess", x => new { x.ticked_id, x.employed_id });
                    table.ForeignKey(
                        name: "fk_ticket_employess_employess_employed_id",
                        column: x => x.employed_id,
                        principalTable: "employess",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ticket_employess_tickects_ticked_id",
                        column: x => x.ticked_id,
                        principalTable: "tickects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ticket_type",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "FatalError" },
                    { 2, "WarningError" },
                    { 3, "NotFoundDeviceError" },
                    { 4, "InternalDeviceError" },
                    { 5, "ManagePersonError" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_ticket_employess_employed_id",
                table: "ticket_employess",
                column: "employed_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_employess");

            migrationBuilder.DropTable(
                name: "ticket_type");

            migrationBuilder.DropTable(
                name: "employess");

            migrationBuilder.DropTable(
                name: "tickects");
        }
    }
}
