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
        // "LastUpdated" is mapped to a Postgres "timestamp without time zone"
        // column. Npgsql (v6+) requires DateTimeKind.Unspecified for that
        // column type - passing a Kind=Utc value throws "Cannot write
        // DateTime with Kind=UTC to PostgreSQL type 'timestamp without time
        // zone'". We still want the UTC instant, just with the Kind flag
        // stripped so Npgsql accepts it.
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