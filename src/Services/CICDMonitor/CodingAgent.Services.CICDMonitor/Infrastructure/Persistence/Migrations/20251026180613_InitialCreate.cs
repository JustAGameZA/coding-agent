using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodingAgent.Services.CICDMonitor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "cicd_monitor");

        migrationBuilder.CreateTable(
            name: "build_failures",
            schema: "cicd_monitor",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Repository = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Branch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CommitSha = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                ErrorMessage = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                ErrorLog = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: true),
                WorkflowName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                JobName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ErrorPattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_build_failures", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "fix_attempts",
            schema: "cicd_monitor",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BuildFailureId = table.Column<Guid>(type: "uuid", nullable: false),
                TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                Repository = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ErrorMessage = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                ErrorPattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Status = table.Column<string>(type: "text", nullable: false),
                AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                PullRequestNumber = table.Column<int>(type: "integer", nullable: true),
                PullRequestUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fix_attempts", x => x.Id);
                table.ForeignKey(
                    name: "FK_fix_attempts_build_failures_BuildFailureId",
                    column: x => x.BuildFailureId,
                    principalSchema: "cicd_monitor",
                    principalTable: "build_failures",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_build_failures_ErrorPattern",
            schema: "cicd_monitor",
            table: "build_failures",
            column: "ErrorPattern");

        migrationBuilder.CreateIndex(
            name: "IX_build_failures_FailedAt",
            schema: "cicd_monitor",
            table: "build_failures",
            column: "FailedAt");

        migrationBuilder.CreateIndex(
            name: "IX_build_failures_Repository",
            schema: "cicd_monitor",
            table: "build_failures",
            column: "Repository");

        migrationBuilder.CreateIndex(
            name: "IX_fix_attempts_AttemptedAt",
            schema: "cicd_monitor",
            table: "fix_attempts",
            column: "AttemptedAt");

        migrationBuilder.CreateIndex(
            name: "IX_fix_attempts_BuildFailureId",
            schema: "cicd_monitor",
            table: "fix_attempts",
            column: "BuildFailureId");

        migrationBuilder.CreateIndex(
            name: "IX_fix_attempts_ErrorPattern",
            schema: "cicd_monitor",
            table: "fix_attempts",
            column: "ErrorPattern");

        migrationBuilder.CreateIndex(
            name: "IX_fix_attempts_Status",
            schema: "cicd_monitor",
            table: "fix_attempts",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_fix_attempts_TaskId",
            schema: "cicd_monitor",
            table: "fix_attempts",
            column: "TaskId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "fix_attempts",
            schema: "cicd_monitor");

        migrationBuilder.DropTable(
            name: "build_failures",
            schema: "cicd_monitor");
    }
}
