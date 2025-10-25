using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodingAgent.Services.Orchestration.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddExecutionResultEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CostUSD",
            schema: "orchestration",
            table: "executions");

        migrationBuilder.DropColumn(
            name: "Duration",
            schema: "orchestration",
            table: "executions");

        migrationBuilder.DropColumn(
            name: "TokensUsed",
            schema: "orchestration",
            table: "executions");

        migrationBuilder.RenameColumn(
            name: "Result",
            schema: "orchestration",
            table: "executions",
            newName: "Status");

        migrationBuilder.RenameIndex(
            name: "IX_executions_Result",
            schema: "orchestration",
            table: "executions",
            newName: "IX_executions_Status");

        migrationBuilder.CreateTable(
            name: "execution_results",
            schema: "orchestration",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                Success = table.Column<bool>(type: "boolean", nullable: false),
                Changes = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: true),
                TokensUsed = table.Column<int>(type: "integer", nullable: false),
                CostUSD = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                ErrorDetails = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                FilesChanged = table.Column<int>(type: "integer", nullable: false),
                LinesAdded = table.Column<int>(type: "integer", nullable: false),
                LinesRemoved = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_execution_results", x => x.Id);
                table.ForeignKey(
                    name: "FK_execution_results_executions_ExecutionId",
                    column: x => x.ExecutionId,
                    principalSchema: "orchestration",
                    principalTable: "executions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_execution_results_ExecutionId",
            schema: "orchestration",
            table: "execution_results",
            column: "ExecutionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_execution_results_Success",
            schema: "orchestration",
            table: "execution_results",
            column: "Success");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "execution_results",
            schema: "orchestration");

        migrationBuilder.RenameColumn(
            name: "Status",
            schema: "orchestration",
            table: "executions",
            newName: "Result");

        migrationBuilder.RenameIndex(
            name: "IX_executions_Status",
            schema: "orchestration",
            table: "executions",
            newName: "IX_executions_Result");

        migrationBuilder.AddColumn<decimal>(
            name: "CostUSD",
            schema: "orchestration",
            table: "executions",
            type: "numeric(18,6)",
            precision: 18,
            scale: 6,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<TimeSpan>(
            name: "Duration",
            schema: "orchestration",
            table: "executions",
            type: "interval",
            nullable: false,
            defaultValue: new TimeSpan(0, 0, 0, 0, 0));

        migrationBuilder.AddColumn<int>(
            name: "TokensUsed",
            schema: "orchestration",
            table: "executions",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }
}
