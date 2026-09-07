using System.Net;
using System.Net.Http.Json;
namespace TmsApi.Tests;
public class CoursesApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    public CoursesApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
    [Fact]
    public async Task GetCourses_ReturnsOkAndPagedJson()
    {
        var response = await _client.GetAsync("/api/v2.0/courses?page=1&pageSize=10");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedCoursesJson>();
        Assert.NotNull(page?.Data);
    }
    [Fact]
    public async Task GetCourse_UnknownCode_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v2.0/courses/ZZZ-999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    private sealed class PagedCoursesJson
    {
        public List<CourseRowJson> Data { get; set; } = default!;
        public MetaJson Meta { get; set; } = default!;
    }
    private sealed class MetaJson
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
    private sealed class CourseRowJson
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Title { get; set; } = "";
        public int MaxCapacity { get; set; }
        public int EnrollmentCount { get; set; }
    }
}