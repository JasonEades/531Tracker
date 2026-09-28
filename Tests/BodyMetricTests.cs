using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace WorkoutExportTests;

public sealed class BodyMetricTests
{
    [Fact]
    public void PhotoProcessorDownscalesAndCompressesOversizedUploads()
    {
        var options = Options.Create(new ProgressPhotoOptions
        {
            MaxDimension = 1280,
            ThumbnailDimension = 320,
            JpegQuality = 78,
            ThumbnailJpegQuality = 65
        });

        using var upload = CreateJpeg(3000, 1500);
        var original = upload.Length;

        var processed = new ProgressPhotoProcessor(options).Process(upload);

        Assert.Equal(1280, processed.Width);
        Assert.Equal(640, processed.Height);
        Assert.True(processed.Image.Length < original, "Stored image should be smaller than the upload.");
        Assert.True(processed.Thumbnail.Length < processed.Image.Length, "Thumbnail should be smaller than the stored image.");
    }

    [Fact]
    public void PhotoProcessorLeavesSmallImagesAtTheirOriginalSize()
    {
        var options = Options.Create(new ProgressPhotoOptions { MaxDimension = 1280, ThumbnailDimension = 320 });

        using var upload = CreateJpeg(600, 800);
        var processed = new ProgressPhotoProcessor(options).Process(upload);

        Assert.Equal(600, processed.Width);
        Assert.Equal(800, processed.Height);
    }

    [Fact]
    public async Task UpsertReplacesTheEntryForTheSameDayInsteadOfDuplicating()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var day = DateTime.Today;

        await fixture.Service.UpsertAsync(new BodyMetricEntry { RecordedOn = day, WeightLb = 200, WaistInches = 34 });
        await fixture.Service.UpsertAsync(new BodyMetricEntry { RecordedOn = day, WeightLb = 198 });

        var entries = await fixture.Service.GetEntriesAsync();
        var entry = Assert.Single(entries);
        Assert.Equal(198, entry.WeightLb);
        Assert.Null(entry.WaistInches);
    }

    [Fact]
    public async Task TrendsCompareTheLatestReadingWithThirtyAndNinetyDayBaselines()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var today = DateTime.Today;

        await fixture.Service.UpsertAsync(new BodyMetricEntry { RecordedOn = today.AddDays(-120), WeightLb = 220 });
        await fixture.Service.UpsertAsync(new BodyMetricEntry { RecordedOn = today.AddDays(-35), WeightLb = 210 });
        await fixture.Service.UpsertAsync(new BodyMetricEntry { RecordedOn = today, WeightLb = 200 });

        var trends = await fixture.Service.GetTrendsAsync();
        var weight = Assert.Single(trends, x => x.Label == "Bodyweight");

        Assert.Equal(200, weight.Latest);
        Assert.Equal(-10, weight.ChangeOver30Days);
        Assert.Equal(-20, weight.ChangeOver90Days);
    }

    [Fact]
    public async Task EntriesAndDeletesAreScopedToTheSignedInUser()
    {
        await using var fixture = await TestFixture.CreateAsync();

        fixture.Context.BodyMetricEntries.Add(new BodyMetricEntry
        {
            UserId = "other-user",
            RecordedOn = DateTime.Today,
            WeightLb = 300
        });
        await fixture.Context.SaveChangesAsync();

        Assert.Empty(await fixture.Service.GetEntriesAsync());

        var foreignId = fixture.Context.BodyMetricEntries.Single(x => x.UserId == "other-user").Id;
        Assert.False(await fixture.Service.DeleteEntryAsync(foreignId));
    }

    [Fact]
    public async Task PhotoUploadsStopAtTheConfiguredPerUserLimit()
    {
        await using var fixture = await TestFixture.CreateAsync(new ProgressPhotoOptions
        {
            MaxPhotosPerUser = 2,
            MaxUploadMegabytes = 15,
            MaxDimension = 640,
            ThumbnailDimension = 160
        });

        Assert.True((await fixture.Service.AddPhotoAsync(CreateJpeg(800, 600), 0, DateTime.Today, null)).Success);
        Assert.True((await fixture.Service.AddPhotoAsync(CreateJpeg(800, 600), 0, DateTime.Today, null)).Success);

        var blocked = await fixture.Service.AddPhotoAsync(CreateJpeg(800, 600), 0, DateTime.Today, null);
        Assert.False(blocked.Success);
        Assert.Contains("limit", blocked.Error!, StringComparison.OrdinalIgnoreCase);

        var quota = await fixture.Service.GetPhotoQuotaAsync();
        Assert.True(quota.IsFull);
        Assert.Equal(0, quota.Remaining);

        // Deleting frees a slot and removes the stored bytes with the row.
        var photo = (await fixture.Service.GetPhotosAsync())[0];
        Assert.True(await fixture.Service.DeletePhotoAsync(photo.Id));
        Assert.Empty(fixture.Context.ProgressPhotos.Where(x => x.Id == photo.Id));
        Assert.True((await fixture.Service.AddPhotoAsync(CreateJpeg(800, 600), 0, DateTime.Today, null)).Success);
    }

    [Fact]
    public async Task StoredPhotoBytesAreServedBackToTheOwner()
    {
        await using var fixture = await TestFixture.CreateAsync(new ProgressPhotoOptions
        {
            MaxDimension = 640,
            ThumbnailDimension = 160
        });

        var upload = await fixture.Service.AddPhotoAsync(CreateJpeg(1600, 1200), 0, DateTime.Today, "front");
        Assert.True(upload.Success);

        var full = await fixture.Service.OpenPhotoAsync(upload.Photo!.Id, thumbnail: false, "test-user");
        var thumb = await fixture.Service.OpenPhotoAsync(upload.Photo.Id, thumbnail: true, "test-user");

        Assert.NotNull(full);
        Assert.NotNull(thumb);
        Assert.Equal("image/jpeg", full!.Value.ContentType);
        Assert.True(thumb!.Value.Content.Length < full.Value.Content.Length);

        // Listing must not drag the image bytes along.
        var listed = Assert.Single(await fixture.Service.GetPhotosAsync());
        Assert.Equal("front", listed.Caption);
        Assert.Equal(full.Value.Content.Length, listed.SizeBytes);
    }

    [Fact]
    public async Task OversizedUploadsAreRejectedBeforeDecoding()
    {
        await using var fixture = await TestFixture.CreateAsync(new ProgressPhotoOptions { MaxUploadMegabytes = 1 });

        var result = await fixture.Service.AddPhotoAsync(CreateJpeg(100, 100), 5L * 1024 * 1024, DateTime.Today, null);

        Assert.False(result.Success);
        Assert.Contains("1 MB", result.Error!);
        Assert.Empty(fixture.Context.ProgressPhotos);
    }

    [Fact]
    public async Task NonImageUploadsFailCleanlyWithoutStoringAnything()
    {
        await using var fixture = await TestFixture.CreateAsync();

        using var garbage = new MemoryStream("not an image"u8.ToArray());
        var result = await fixture.Service.AddPhotoAsync(garbage, garbage.Length, DateTime.Today, null);

        Assert.False(result.Success);
        Assert.Empty(fixture.Context.ProgressPhotos);
        Assert.Empty(await fixture.Service.GetPhotosAsync());
    }

    [Fact]
    public async Task PhotosCannotBeReadOrDeletedByAnotherUser()
    {
        await using var fixture = await TestFixture.CreateAsync();

        fixture.Context.ProgressPhotos.Add(new ProgressPhoto
        {
            UserId = "other-user",
            TakenOn = DateTime.Today,
            ImageData = [1, 2, 3],
            ThumbnailData = [1, 2]
        });
        await fixture.Context.SaveChangesAsync();

        var foreignId = fixture.Context.ProgressPhotos.Single(x => x.UserId == "other-user").Id;

        Assert.Null(await fixture.Service.OpenPhotoAsync(foreignId, thumbnail: false, "test-user"));
        Assert.False(await fixture.Service.DeletePhotoAsync(foreignId));
    }

    private static MemoryStream CreateJpeg(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32((byte)(x % 256), (byte)(y % 256), (byte)((x + y) % 256));
            }
        }

        var stream = new MemoryStream();
        image.Save(stream, new JpegEncoder { Quality = 95 });
        stream.Position = 0;
        return stream;
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Task<string> GetUserIdAsync() => Task.FromResult("test-user");
        public Task<string?> GetUserIdOrNullAsync() => Task.FromResult<string?>("test-user");
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private SqliteConnection connection = null!;

        public AppDbContext Context { get; private set; } = null!;
        public BodyMetricService Service { get; private set; } = null!;

        public static async Task<TestFixture> CreateAsync(ProgressPhotoOptions? options = null)
        {
            var fixture = new TestFixture();
            fixture.connection = new SqliteConnection("Data Source=:memory:");
            await fixture.connection.OpenAsync();

            var contextOptions = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(fixture.connection).Options;
            fixture.Context = new AppDbContext(contextOptions);
            await fixture.Context.Database.EnsureCreatedAsync();

            // Body metric rows are FK-constrained to the identity user table.
            fixture.Context.Users.AddRange(
                new ApplicationUser { Id = "test-user", UserName = "test-user", Email = "test@example.com" },
                new ApplicationUser { Id = "other-user", UserName = "other-user", Email = "other@example.com" });
            await fixture.Context.SaveChangesAsync();

            var photoOptions = Options.Create(options ?? new ProgressPhotoOptions());
            fixture.Service = new BodyMetricService(
                fixture.Context,
                new TestCurrentUserService(),
                new ProgressPhotoProcessor(photoOptions),
                photoOptions);

            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
