using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ConcertTicketsApi.Data;

namespace ConcertTicketsApi.Migrations;

[DbContext(typeof(AppDbContext))]
public sealed class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) =>
        ConcertTicketsModel.Configure(modelBuilder, hasSectorVersion: true, hasFinalIndexes: true);
}
