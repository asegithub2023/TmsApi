using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;
namespace TmsApi.Infrastructure.Persistence.Configurations;
public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);
        // Restrict deletes so enrollment history is not removed accidentally.
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.StudentId, e.CourseId })
            // A student may enroll in a course only once.
            .IsUnique();
        builder.Property(e => e.Grade)
            .HasPrecision(5, 2);
        builder.Property(e => e.EnrolledAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EnrollmentStatus.Pending);
    }
}