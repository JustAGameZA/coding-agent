using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodingAgent.Services.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignAuthSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_api_keys_users_UserId",
                schema: "auth",
                table: "api_keys");

            migrationBuilder.DropForeignKey(
                name: "FK_sessions_users_UserId",
                schema: "auth",
                table: "sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                schema: "auth",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sessions",
                schema: "auth",
                table: "sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_api_keys",
                schema: "auth",
                table: "api_keys");

            migrationBuilder.RenameColumn(
                name: "Username",
                schema: "auth",
                table: "users",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Roles",
                schema: "auth",
                table: "users",
                newName: "roles");

            migrationBuilder.RenameColumn(
                name: "Email",
                schema: "auth",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "auth",
                table: "users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "auth",
                table: "users",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                schema: "auth",
                table: "users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                schema: "auth",
                table: "users",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "auth",
                table: "users",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_users_Username",
                schema: "auth",
                table: "users",
                newName: "ix_users_username");

            migrationBuilder.RenameIndex(
                name: "IX_users_Email",
                schema: "auth",
                table: "users",
                newName: "ix_users_email");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "auth",
                table: "sessions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "auth",
                table: "sessions",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UserAgent",
                schema: "auth",
                table: "sessions",
                newName: "user_agent");

            migrationBuilder.RenameColumn(
                name: "RevokedAt",
                schema: "auth",
                table: "sessions",
                newName: "revoked_at");

            migrationBuilder.RenameColumn(
                name: "RefreshTokenHash",
                schema: "auth",
                table: "sessions",
                newName: "refresh_token_hash");

            migrationBuilder.RenameColumn(
                name: "IsRevoked",
                schema: "auth",
                table: "sessions",
                newName: "is_revoked");

            migrationBuilder.RenameColumn(
                name: "IpAddress",
                schema: "auth",
                table: "sessions",
                newName: "ip_address");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "auth",
                table: "sessions",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "auth",
                table: "sessions",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_sessions_UserId",
                schema: "auth",
                table: "sessions",
                newName: "ix_sessions_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_sessions_RefreshTokenHash",
                schema: "auth",
                table: "sessions",
                newName: "ix_sessions_refresh_token_hash");

            migrationBuilder.RenameIndex(
                name: "IX_sessions_IsRevoked_ExpiresAt",
                schema: "auth",
                table: "sessions",
                newName: "ix_sessions_is_revoked_expires_at");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "auth",
                table: "api_keys",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "auth",
                table: "api_keys",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "auth",
                table: "api_keys",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "RevokedAt",
                schema: "auth",
                table: "api_keys",
                newName: "revoked_at");

            migrationBuilder.RenameColumn(
                name: "LastUsedAt",
                schema: "auth",
                table: "api_keys",
                newName: "last_used_at");

            migrationBuilder.RenameColumn(
                name: "KeyHash",
                schema: "auth",
                table: "api_keys",
                newName: "key_hash");

            migrationBuilder.RenameColumn(
                name: "IsRevoked",
                schema: "auth",
                table: "api_keys",
                newName: "is_revoked");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "auth",
                table: "api_keys",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "auth",
                table: "api_keys",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_api_keys_UserId",
                schema: "auth",
                table: "api_keys",
                newName: "ix_api_keys_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_api_keys_KeyHash",
                schema: "auth",
                table: "api_keys",
                newName: "ix_api_keys_key_hash");

            migrationBuilder.RenameIndex(
                name: "IX_api_keys_IsRevoked_ExpiresAt",
                schema: "auth",
                table: "api_keys",
                newName: "ix_api_keys_is_revoked_expires_at");

            migrationBuilder.AddPrimaryKey(
                name: "pk_users",
                schema: "auth",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sessions",
                schema: "auth",
                table: "sessions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_api_keys",
                schema: "auth",
                table: "api_keys",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_api_keys_users_user_id",
                schema: "auth",
                table: "api_keys",
                column: "user_id",
                principalSchema: "auth",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sessions_users_user_id",
                schema: "auth",
                table: "sessions",
                column: "user_id",
                principalSchema: "auth",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_api_keys_users_user_id",
                schema: "auth",
                table: "api_keys");

            migrationBuilder.DropForeignKey(
                name: "fk_sessions_users_user_id",
                schema: "auth",
                table: "sessions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_users",
                schema: "auth",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sessions",
                schema: "auth",
                table: "sessions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_api_keys",
                schema: "auth",
                table: "api_keys");

            migrationBuilder.RenameColumn(
                name: "username",
                schema: "auth",
                table: "users",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "roles",
                schema: "auth",
                table: "users",
                newName: "Roles");

            migrationBuilder.RenameColumn(
                name: "email",
                schema: "auth",
                table: "users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "auth",
                table: "users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "auth",
                table: "users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                schema: "auth",
                table: "users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "is_active",
                schema: "auth",
                table: "users",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "auth",
                table: "users",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_users_username",
                schema: "auth",
                table: "users",
                newName: "IX_users_Username");

            migrationBuilder.RenameIndex(
                name: "ix_users_email",
                schema: "auth",
                table: "users",
                newName: "IX_users_Email");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "auth",
                table: "sessions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "auth",
                table: "sessions",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "user_agent",
                schema: "auth",
                table: "sessions",
                newName: "UserAgent");

            migrationBuilder.RenameColumn(
                name: "revoked_at",
                schema: "auth",
                table: "sessions",
                newName: "RevokedAt");

            migrationBuilder.RenameColumn(
                name: "refresh_token_hash",
                schema: "auth",
                table: "sessions",
                newName: "RefreshTokenHash");

            migrationBuilder.RenameColumn(
                name: "is_revoked",
                schema: "auth",
                table: "sessions",
                newName: "IsRevoked");

            migrationBuilder.RenameColumn(
                name: "ip_address",
                schema: "auth",
                table: "sessions",
                newName: "IpAddress");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                schema: "auth",
                table: "sessions",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "auth",
                table: "sessions",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_sessions_user_id",
                schema: "auth",
                table: "sessions",
                newName: "IX_sessions_UserId");

            migrationBuilder.RenameIndex(
                name: "ix_sessions_refresh_token_hash",
                schema: "auth",
                table: "sessions",
                newName: "IX_sessions_RefreshTokenHash");

            migrationBuilder.RenameIndex(
                name: "ix_sessions_is_revoked_expires_at",
                schema: "auth",
                table: "sessions",
                newName: "IX_sessions_IsRevoked_ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "auth",
                table: "api_keys",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "auth",
                table: "api_keys",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "auth",
                table: "api_keys",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "revoked_at",
                schema: "auth",
                table: "api_keys",
                newName: "RevokedAt");

            migrationBuilder.RenameColumn(
                name: "last_used_at",
                schema: "auth",
                table: "api_keys",
                newName: "LastUsedAt");

            migrationBuilder.RenameColumn(
                name: "key_hash",
                schema: "auth",
                table: "api_keys",
                newName: "KeyHash");

            migrationBuilder.RenameColumn(
                name: "is_revoked",
                schema: "auth",
                table: "api_keys",
                newName: "IsRevoked");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                schema: "auth",
                table: "api_keys",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "auth",
                table: "api_keys",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_api_keys_user_id",
                schema: "auth",
                table: "api_keys",
                newName: "IX_api_keys_UserId");

            migrationBuilder.RenameIndex(
                name: "ix_api_keys_key_hash",
                schema: "auth",
                table: "api_keys",
                newName: "IX_api_keys_KeyHash");

            migrationBuilder.RenameIndex(
                name: "ix_api_keys_is_revoked_expires_at",
                schema: "auth",
                table: "api_keys",
                newName: "IX_api_keys_IsRevoked_ExpiresAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                schema: "auth",
                table: "users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sessions",
                schema: "auth",
                table: "sessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_api_keys",
                schema: "auth",
                table: "api_keys",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_api_keys_users_UserId",
                schema: "auth",
                table: "api_keys",
                column: "UserId",
                principalSchema: "auth",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sessions_users_UserId",
                schema: "auth",
                table: "sessions",
                column: "UserId",
                principalSchema: "auth",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
