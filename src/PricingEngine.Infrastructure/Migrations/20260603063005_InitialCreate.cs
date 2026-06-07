using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PricingEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LockedAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConfigKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfigValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quote_audit_log",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_audit_log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    input_data = table.Column<string>(type: "jsonb", nullable: false),
                    NetPremium = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Taxes = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Fees = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    installment_plans = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "product_configs",
                columns: new[] { "Id", "ConfigKey", "ConfigValue", "EffectiveFrom", "EffectiveTo", "ProductCode" },
                values: new object[,]
                {
                    { new Guid("a1000001-0000-0000-0000-000000000000"), "tariff_rate", "0.0018", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "HOME_BASIC" },
                    { new Guid("a1000002-0000-0000-0000-000000000000"), "fixed_fee", "25.00", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "HOME_BASIC" },
                    { new Guid("a1000003-0000-0000-0000-000000000000"), "tax_rate", "0.12", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "HOME_BASIC" },
                    { new Guid("a1000004-0000-0000-0000-000000000000"), "installment_fee_2x", "0.015", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "HOME_BASIC" },
                    { new Guid("a1000005-0000-0000-0000-000000000000"), "installment_fee_4x", "0.030", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "HOME_BASIC" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_unprocessed",
                table: "outbox_messages",
                columns: new[] { "ProcessedAt", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "ix_product_configs_product_key",
                table: "product_configs",
                columns: new[] { "ProductCode", "ConfigKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_quote_id",
                table: "quote_audit_log",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "ix_quotes_created_at",
                table: "quotes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_quotes_product_code",
                table: "quotes",
                column: "ProductCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "product_configs");

            migrationBuilder.DropTable(
                name: "quote_audit_log");

            migrationBuilder.DropTable(
                name: "quotes");
        }
    }
}
