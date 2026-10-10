using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ConcertTicketsApi.Data;

namespace ConcertTicketsApi.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202610100002_SectorVersionAndIndexes")]
public sealed class SectorVersionAndIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "Version",
            table: "Sectors",
            type: "TEXT",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.CreateIndex(name: "IX_Tickets_Number", table: "Tickets", column: "Number", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Buyers_Email", table: "Buyers", column: "Email", unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_ConcertEvents_Status_StartsAt_Id",
            table: "ConcertEvents",
            columns: new[] { "Status", "StartsAt", "Id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Tickets_Number", table: "Tickets");
        migrationBuilder.DropIndex(name: "IX_Buyers_Email", table: "Buyers");
        migrationBuilder.DropIndex(name: "IX_ConcertEvents_Status_StartsAt_Id", table: "ConcertEvents");
        migrationBuilder.DropColumn(name: "Version", table: "Sectors");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder) =>
        ConcertTicketsModel.Configure(modelBuilder, hasSectorVersion: true, hasFinalIndexes: true);
}
