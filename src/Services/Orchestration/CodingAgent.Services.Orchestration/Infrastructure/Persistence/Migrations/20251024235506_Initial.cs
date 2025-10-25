using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodingAgent.Services.Orchestration.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "orchestration");

        migrationBuilder.CreateTable(
            name: "tasks",
            schema: "orchestration",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                Type = table.Column<string>(type: "text", nullable: false),
                Complexity = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tasks", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "executions",
            schema: "orchestration",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                Strategy = table.Column<string>(type: "text", nullable: false),
                ModelUsed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TokensUsed = table.Column<int>(type: "integer", nullable: false),
                CostUSD = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                Result = table.Column<string>(type: "text", nullable: false),
                ErrorMessage = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_executions", x => x.Id);
                table.ForeignKey(
                    name: "FK_executions_tasks_TaskId",
                    column: x => x.TaskId,
                    principalSchema: "orchestration",
                    principalTable: "tasks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_executions_Result",
            schema: "orchestration",
            table: "executions",
            column: "Result");

        migrationBuilder.CreateIndex(
            name: "IX_executions_StartedAt",
            schema: "orchestration",
            table: "executions",
            column: "StartedAt");

        migrationBuilder.CreateIndex(
            name: "IX_executions_TaskId",
            schema: "orchestration",
            table: "executions",
            column: "TaskId");

        migrationBuilder.CreateIndex(
            name: "IX_tasks_CreatedAt",
            schema: "orchestration",
            table: "tasks",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_tasks_Status",
            schema: "orchestration",
            table: "tasks",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_tasks_UserId",
            schema: "orchestration",
            table: "tasks",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_tasks_UserId_Status",
            schema: "orchestration",
            table: "tasks",
            columns: new[] { "UserId", "Status" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "executions",
            schema: "orchestration");

        migrationBuilder.DropTable(
            name: "tasks",
            schema: "orchestration");
    }
}
