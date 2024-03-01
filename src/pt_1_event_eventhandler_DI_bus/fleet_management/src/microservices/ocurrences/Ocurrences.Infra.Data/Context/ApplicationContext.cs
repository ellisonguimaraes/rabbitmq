using Microsoft.EntityFrameworkCore;
using Ocurrences.Domain.Core.Entities;

namespace Ocurrences.Infra.Data.Context;

public class ApplicationContext(DbContextOptions<ApplicationContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ocurrence>().HasKey(o => o.Id);
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<Ocurrence> Ocurrences { get; set; }
}
