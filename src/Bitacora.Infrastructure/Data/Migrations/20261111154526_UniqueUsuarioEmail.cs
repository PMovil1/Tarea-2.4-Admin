using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bitacora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniqueUsuarioEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Detener antes de modificar datos si hace falta una revisión manual.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Usuarios WHERE LEN(Email) > 254 OR LEN(LTRIM(RTRIM(Email))) = 0)
                    THROW 50001, 'Revisar correos vacios o mayores de 254 caracteres antes de migrar.', 1;
                IF EXISTS (SELECT UPPER(LTRIM(RTRIM(Email))) FROM Usuarios
                           GROUP BY UPPER(LTRIM(RTRIM(Email))) HAVING COUNT(*) > 1)
                    THROW 50002, 'Resolver correos duplicados antes de migrar; no se eliminaron usuarios.', 1;
                UPDATE Usuarios SET Email = UPPER(LTRIM(RTRIM(Email)));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Usuarios",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254);
        }
    }
}
