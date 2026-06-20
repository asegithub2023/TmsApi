var count = await context.Students
.Where(s => s.IsActive && s.GPA >= 3.0m)
.CountAsync();


var list = await context.Courses
.Select(c => new
{
c.Title,
EnrollmentCount = c.Enrollments.Count
})
.OrderByDescending(x => x.EnrollmentCount)
.ToListAsync();


var list = await context.Enrollments
.GroupBy(e => e.Course.Title)
.Select(g => new
{
Course = g.Key,
AverageGPA = g.Average(e => e.Student.GPA)
})
.ToListAsync();


var list = await context.Students
.Where(s => !s.Enrollments.Any())
.Select(s => s.Name)
.ToListAsync();


var list = await context.Students
.LeftJoin(context.Enrollments,
s => s.Id,
e => e.StudentId,
(s, e) => new { s, e })
.Where(x => x.e == null)
.Select(x => x.s.Name)
.ToListAsync();


