using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineVotingSystem.Migrations
{
    /// <inheritdoc />
    public partial class Fixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptionType",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "ServerHalf",
                table: "UserImages",
                newName: "UserHalfEncrypted");

            migrationBuilder.RenameColumn(
                name: "EncryptedHalf",
                table: "UserImages",
                newName: "ServerHalfEncrypted");

            migrationBuilder.AddColumn<string>(
                name: "EncryptionMethod",
                table: "UserImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "RawCaptcha",
                table: "UserImages",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "ServerHalfIV",
                table: "UserImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ServerHalfKey",
                table: "UserImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ServerPrivateKey",
                table: "UserImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ServerPublicKey",
                table: "UserImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserHalfIV",
                table: "UserImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserHalfKey",
                table: "UserImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserPrivateKey",
                table: "UserImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserPublicKey",
                table: "UserImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptionMethod",
                table: "UserImages");

            migrationBuilder.DropColumn(
                name: "RawCaptcha",
                table: "UserImages");

            migrationBuilder.DropColumn(
                name: "ServerHalfIV",
                table: "UserImages");

            migrationBuilder.DropColumn(
                name: "ServerHalfKey",
                table: "UserImages");

            migrationBuilder.DropColumn(
                name: "ServerPrivateKey",
                table: "UserImages");

            migrationBuilder.DropColumn(
                name: "ServerPublicKey",
                table: "UserImages");

            migrationBuilder.DropColumn(
                name: "UserHalfIV",
                table: "UserImages");

            migrationBuilder.DropColumn(
                name: "UserHalfKey",
                table: "UserImages");

            migrationBuilder.DropColumn(
                name: "UserPrivateKey",
                table: "UserImages");

            migrationBuilder.DropColumn(
                name: "UserPublicKey",
                table: "UserImages");

            migrationBuilder.RenameColumn(
                name: "UserHalfEncrypted",
                table: "UserImages",
                newName: "ServerHalf");

            migrationBuilder.RenameColumn(
                name: "ServerHalfEncrypted",
                table: "UserImages",
                newName: "EncryptedHalf");

            migrationBuilder.AddColumn<string>(
                name: "EncryptionType",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
