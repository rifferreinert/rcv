using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rcv.Web.Api.Migrations;

/// <inheritdoc />
public partial class RemoveIsVotingPublic : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsVotingPublic",
            table: "Polls");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsVotingPublic",
            table: "Polls",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }
}
