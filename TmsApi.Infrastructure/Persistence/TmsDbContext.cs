using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace TmsApi.Infrastructure.Persistence;
public class TmsDbContext : IdentityDbContext<TmsUser>
{
public TmsDbContext(DbContextOptions<TmsDbContext> options) :
base(options) { }
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public override int SaveChanges()
    {
        // Update audit fields consistently for both synchronous and async writes.
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
        // PostgreSQL timestamp columns use an unspecified DateTime kind by design.
        var timestamp = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
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