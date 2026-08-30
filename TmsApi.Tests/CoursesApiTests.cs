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
        // Act — pin the V2 URL
        var response = await _client.GetAsync("/api/v2.0/courses?page=1&pageSize=10");

        // Assert — check HTTP status 200 OK
        response.EnsureSuccessStatusCode();

        // TMS API contract check: { data: [...], meta: { totalCount, ... } }
        var page = await response.Content.ReadFromJsonAsync<PagedCoursesJson>();
        Assert.NotNull(page?.Data);
    }

    [Fact]
    public async Task GetCourse_UnknownCode_ReturnsNotFound()
    {
        // Act — a code that doesn't exist in the seeded/in-memory data
        var response = await _client.GetAsync("/api/v2.0/courses/ZZZ-999");

        // Assert — the API correctly rejects a lookup for input that doesn't exist
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