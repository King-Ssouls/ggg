using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ConcertTicketsApi.Data;

namespace ConcertTicketsApi.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202610100001_InitialConcertSchema")]
public sealed class InitialConcertSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE "Venues" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Venues" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "Address" TEXT NOT NULL
            );
            CREATE TABLE "Sponsors" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Sponsors" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL
            );
            CREATE TABLE "ConcertEvents" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ConcertEvents" PRIMARY KEY AUTOINCREMENT,
                "Title" TEXT NOT NULL,
                "StartsAt" TEXT NOT NULL,
                "Status" TEXT NOT NULL CHECK("Status" IN ('Scheduled', 'Cancelled')),
                "VenueId" INTEGER NOT NULL,
                "SponsorId" INTEGER NULL,
                CONSTRAINT "FK_ConcertEvents_Venues_VenueId" FOREIGN KEY ("VenueId") REFERENCES "Venues" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_ConcertEvents_Sponsors_SponsorId" FOREIGN KEY ("SponsorId") REFERENCES "Sponsors" ("Id") ON DELETE SET NULL
            );
            CREATE TABLE "Sectors" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Sectors" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "Capacity" INTEGER NOT NULL CHECK("Capacity" > 0),
                "SoldCount" INTEGER NOT NULL CHECK("SoldCount" >= 0 AND "SoldCount" <= "Capacity"),
                "ConcertEventId" INTEGER NOT NULL,
                CONSTRAINT "FK_Sectors_ConcertEvents_ConcertEventId" FOREIGN KEY ("ConcertEventId") REFERENCES "ConcertEvents" ("Id") ON DELETE RESTRICT
            );
            CREATE TABLE "Buyers" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Buyers" PRIMARY KEY AUTOINCREMENT,
                "LastName" TEXT NOT NULL,
                "FirstName" TEXT NOT NULL,
                "Email" TEXT NOT NULL
            );
            CREATE TABLE "PriceCategories" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PriceCategories" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "Price" TEXT NOT NULL CHECK("Price" >= 0),
                "SectorId" INTEGER NOT NULL,
                CONSTRAINT "FK_PriceCategories_Sectors_SectorId" FOREIGN KEY ("SectorId") REFERENCES "Sectors" ("Id") ON DELETE RESTRICT
            );
            CREATE TABLE "Tickets" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Tickets" PRIMARY KEY AUTOINCREMENT,
                "Number" TEXT NOT NULL,
                "Status" TEXT NOT NULL CHECK("Status" IN ('Sold', 'Refunded')),
                "SoldAt" TEXT NOT NULL,
                "SoldPrice" TEXT NOT NULL CHECK("SoldPrice" >= 0),
                "InternalBarcode" TEXT NOT NULL,
                "SectorId" INTEGER NOT NULL,
                "PriceCategoryId" INTEGER NOT NULL,
                "BuyerId" INTEGER NOT NULL,
                CONSTRAINT "FK_Tickets_Sectors_SectorId" FOREIGN KEY ("SectorId") REFERENCES "Sectors" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_Tickets_PriceCategories_PriceCategoryId" FOREIGN KEY ("PriceCategoryId") REFERENCES "PriceCategories" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_Tickets_Buyers_BuyerId" FOREIGN KEY ("BuyerId") REFERENCES "Buyers" ("Id") ON DELETE RESTRICT
            );
            CREATE INDEX "IX_ConcertEvents_VenueId" ON "ConcertEvents" ("VenueId");
            CREATE INDEX "IX_ConcertEvents_SponsorId" ON "ConcertEvents" ("SponsorId");
            CREATE UNIQUE INDEX "IX_Sectors_ConcertEventId_Name" ON "Sectors" ("ConcertEventId", "Name");
            CREATE INDEX "IX_PriceCategories_SectorId" ON "PriceCategories" ("SectorId");
            CREATE INDEX "IX_Tickets_SectorId" ON "Tickets" ("SectorId");
            CREATE INDEX "IX_Tickets_PriceCategoryId" ON "Tickets" ("PriceCategoryId");
            CREATE INDEX "IX_Tickets_BuyerId" ON "Tickets" ("BuyerId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE "Tickets";
            DROP TABLE "PriceCategories";
            DROP TABLE "Buyers";
            DROP TABLE "Sectors";
            DROP TABLE "ConcertEvents";
            DROP TABLE "Sponsors";
            DROP TABLE "Venues";
            """);
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder) =>
        ConcertTicketsModel.Configure(modelBuilder, hasSectorVersion: false, hasFinalIndexes: false);
}
