using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence;

public class TmsDbContext(
    DbContextOptions<TmsDbContext> options)
    : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public DbSet<Assessment> Assessments => Set<Assessment>();

    public DbSet<Certificate> Certificates => Set<Certificate>();
    
    public override int SaveChanges()
    {
        UpdateShadowProperties();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        UpdateShadowProperties();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateShadowProperties()
    {
        var timestamp = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Student>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Property("LastUpdated").CurrentValue = timestamp;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly);
    }
}