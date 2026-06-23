using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);

        // Foreign key configuration - explicit relationship setup
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);  // Cannot delete student with enrollments

        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);  // Cannot delete course with enrollments

        // Unique constraint: one student cannot enroll in same course twice
        builder.HasIndex(e => new { e.StudentId, e.CourseId })
            .IsUnique();

        builder.Property(e => e.Grade)
            .HasPrecision(5, 2);

        builder.Property(e => e.EnrolledAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}