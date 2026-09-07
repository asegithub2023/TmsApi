using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;
namespace TmsApi.Infrastructure.Persistence.Configurations;
public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);
        // Course codes are stable public identifiers and must remain unique.
        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(c => c.MaxCapacity)
            .HasDefaultValue(30);
        builder.HasIndex(c => c.Code)
           .IsUnique();
        builder.HasMany(c => c.Enrollments)
            // Preserve the course record while enrollment history exists.
            .WithOne(e => e.Course)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}