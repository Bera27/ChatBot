using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Chatbot.Migrations
{
    /// <inheritdoc />
    public partial class SeedInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Gatilhos",
                columns: new[] { "Id", "Nome" },
                values: new object[] { 1, "Horário" });

            migrationBuilder.InsertData(
                table: "Chaves",
                columns: new[] { "Id", "IdGatilho", "Texto" },
                values: new object[,]
                {
                    { 1, 1, "horario" },
                    { 2, 1, "horas" }
                });

            migrationBuilder.InsertData(
                table: "Respostas",
                columns: new[] { "Id", "IdGatilho", "Texto" },
                values: new object[] { 1, 1, "Funcionamos de segunda a sexta, das 8h às 18h." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Chaves",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Chaves",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Respostas",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Gatilhos",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
