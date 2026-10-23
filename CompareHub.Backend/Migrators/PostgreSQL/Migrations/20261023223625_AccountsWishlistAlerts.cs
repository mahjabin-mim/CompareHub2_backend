using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompareHub.Backend.Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AccountsWishlistAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_source_links_app_users_UserId",
                table: "user_source_links");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "user_source_links",
                newName: "url");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "user_source_links",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WebsiteName",
                table: "user_source_links",
                newName: "website_name");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "user_source_links",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "user_source_links",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "user_source_links",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_user_source_links_UserId_IsActive",
                table: "user_source_links",
                newName: "IX_user_source_links_user_id_is_active");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "app_users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "app_users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "app_users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "app_users",
                newName: "full_name");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "app_users",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_app_users_Email",
                table: "app_users",
                newName: "IX_app_users_email");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "app_users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "email_alerts_enabled",
                table: "app_users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "google_subject",
                table: "app_users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "push_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    p256dh = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    auth = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_push_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_push_subscriptions_app_users_user_id",
                        column: x => x.user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_product_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    base_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    search_endpoint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    query_param_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    http_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    api_key_encrypted = table.Column<string>(type: "text", nullable: false),
                    headers_json = table.Column<string>(type: "text", nullable: false),
                    name_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    price_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    image_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    product_url_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_product_sources", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_product_sources_app_users_user_id",
                        column: x => x.user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wishlist_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    image_urls_json = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wishlist_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_wishlist_items_app_users_user_id",
                        column: x => x.user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "price_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wishlist_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wishlist_offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    old_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    new_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_alerts", x => x.id);
                    table.ForeignKey(
                        name: "FK_price_alerts_wishlist_items_wishlist_item_id",
                        column: x => x.wishlist_item_id,
                        principalTable: "wishlist_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wishlist_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wishlist_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    product_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    saved_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    current_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wishlist_offers", x => x.id);
                    table.ForeignKey(
                        name: "FK_wishlist_offers_wishlist_items_wishlist_item_id",
                        column: x => x.wishlist_item_id,
                        principalTable: "wishlist_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "price_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wishlist_offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_points", x => x.id);
                    table.ForeignKey(
                        name: "FK_price_points_wishlist_offers_wishlist_offer_id",
                        column: x => x.wishlist_offer_id,
                        principalTable: "wishlist_offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_users_google_subject",
                table: "app_users",
                column: "google_subject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_price_alerts_user_id_created_at",
                table: "price_alerts",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_price_alerts_wishlist_item_id",
                table: "price_alerts",
                column: "wishlist_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_points_wishlist_offer_id_recorded_at",
                table: "price_points",
                columns: new[] { "wishlist_offer_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "IX_push_subscriptions_endpoint",
                table: "push_subscriptions",
                column: "endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_push_subscriptions_user_id",
                table: "push_subscriptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_product_sources_user_id_is_active",
                table: "user_product_sources",
                columns: new[] { "user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_items_user_id",
                table: "wishlist_items",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_offers_wishlist_item_id",
                table: "wishlist_offers",
                column: "wishlist_item_id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_source_links_app_users_user_id",
                table: "user_source_links",
                column: "user_id",
                principalTable: "app_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_source_links_app_users_user_id",
                table: "user_source_links");

            migrationBuilder.DropTable(
                name: "price_alerts");

            migrationBuilder.DropTable(
                name: "price_points");

            migrationBuilder.DropTable(
                name: "push_subscriptions");

            migrationBuilder.DropTable(
                name: "user_product_sources");

            migrationBuilder.DropTable(
                name: "wishlist_offers");

            migrationBuilder.DropTable(
                name: "wishlist_items");

            migrationBuilder.DropIndex(
                name: "IX_app_users_google_subject",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "email_alerts_enabled",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "google_subject",
                table: "app_users");

            migrationBuilder.RenameColumn(
                name: "url",
                table: "user_source_links",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "user_source_links",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "website_name",
                table: "user_source_links",
                newName: "WebsiteName");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "user_source_links",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "user_source_links",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "user_source_links",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_user_source_links_user_id_is_active",
                table: "user_source_links",
                newName: "IX_user_source_links_UserId_IsActive");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "app_users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "app_users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "app_users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "full_name",
                table: "app_users",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "app_users",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_app_users_email",
                table: "app_users",
                newName: "IX_app_users_Email");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "app_users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_user_source_links_app_users_UserId",
                table: "user_source_links",
                column: "UserId",
                principalTable: "app_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
