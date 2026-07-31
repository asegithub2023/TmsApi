using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Common;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Caching;

namespace TmsApi.Infrastructure.Services;

public class CachedCourseService(
    HybridCache cache,
    ICourseRepository repo,
    ILogger<CachedCourseService> logger) : ICachedCourseService
{
    public async Task<CourseResponseDto> GetCourseAsync(string code, CancellationToken ct)
    {
        var key = CacheKeys.Course(code);
        var dbHit = false;

        var dto = await cache.GetOrCreateAsync(
            key,
            (repo, code),
            async (state, token) =>
            {
                dbHit = true;
                logger.LogInformation("Cache MISS for {Key} fetching from DB", key);
                TmsMeters.CacheMisses.Add(1, new KeyValuePair<string, object?>("key.kind", "course"));

                var course = await state.repo.GetByCodeAsync(state.code, token)
                    ?? throw new NotFoundException($"Course {state.code} not found.");

                return new CourseResponseDto(
                    course.Id,
                    course.Code,
                    course.Title,
                    course.MaxCapacity,
                    course.Enrollments.Count);
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
        {
            logger.LogInformation("Cache HIT for {Key}", key);
            TmsMeters.CacheHits.Add(1, new KeyValuePair<string, object?>("key.kind", "course"));
        }

        return dto;
    }

    public async Task<List<CourseResponseDto>> GetAllCoursesAsync(CancellationToken ct)
    {
        var key = CacheKeys.CoursesAll;
        var dbHit = false;

        var list = await cache.GetOrCreateAsync(
            key,
            repo,
            async (state, token) =>
            {
                dbHit = true;
                logger.LogInformation("Cache MISS for {Key} fetching from DB", key);
                TmsMeters.CacheMisses.Add(1, new KeyValuePair<string, object?>("key.kind", "course"));

                var courses = await state.GetAllAsync(token);
                return courses.Select(c => new CourseResponseDto(
                    c.Id,
                    c.Code,
                    c.Title,
                    c.MaxCapacity,
                    c.Enrollments.Count)).ToList();
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
        {
            logger.LogInformation("Cache HIT for {Key}", key);
            TmsMeters.CacheHits.Add(1, new KeyValuePair<string, object?>("key.kind", "course"));
        }

        return list;
    }

    public async Task InvalidateCourseCacheAsync(CancellationToken ct)
    {
        logger.LogInformation("Invalidating cache tag {Tag}", CacheKeys.CoursesTag);
        await cache.RemoveByTagAsync(CacheKeys.CoursesTag, ct);
    }
}