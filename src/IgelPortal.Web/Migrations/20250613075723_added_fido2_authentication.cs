using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IgelPortal.Web.Migrations
{
    /// <inheritdoc />
    public partial class added_fido2_authentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fido2PublicKeyCredentials",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "varbinary(3072)", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "longblob", nullable: false),
                    SignatureCounter = table.Column<uint>(type: "int unsigned", nullable: false),
                    IsBackupEligible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsBackedUp = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AttestationObject = table.Column<byte[]>(type: "longblob", nullable: false),
                    AttestationClientDataJson = table.Column<byte[]>(type: "longblob", nullable: false),
                    AttestationFormat = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AaGuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fido2PublicKeyCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fido2PublicKeyCredentials_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Fido2AuthenticatorTransports",
                columns: table => new
                {
                    PublicKeyCredentialId = table.Column<byte[]>(type: "varbinary(200)", maxLength: 200, nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fido2AuthenticatorTransports", x => new { x.PublicKeyCredentialId, x.Value });
                    table.ForeignKey(
                        name: "FK_Fido2AuthenticatorTransports_Fido2PublicKeyCredentials_Publi~",
                        column: x => x.PublicKeyCredentialId,
                        principalTable: "Fido2PublicKeyCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Fido2DevicePublicKeys",
                columns: table => new
                {
                    PublicKeyCredentialId = table.Column<byte[]>(type: "varbinary(255)", maxLength: 255, nullable: false),
                    Value = table.Column<byte[]>(type: "varbinary(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("FK_Fido2DevicePubKeys_PubKeyCreds", x => new { x.PublicKeyCredentialId, x.Value });
                    table.ForeignKey(
                        name: "FK_Fido2DevicePublicKeys_Fido2PublicKeyCredentials_PublicKeyCre~",
                        column: x => x.PublicKeyCredentialId,
                        principalTable: "Fido2PublicKeyCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Fido2PublicKeyCredentials_UserId",
                table: "Fido2PublicKeyCredentials",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fido2AuthenticatorTransports");

            migrationBuilder.DropTable(
                name: "Fido2DevicePublicKeys");

            migrationBuilder.DropTable(
                name: "Fido2PublicKeyCredentials");
        }
    }
}
