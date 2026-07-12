using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Application.Services;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MangaManagementSystem.Application.UnitTests.Services;

public class ChapterPageVersionServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<ChapterPageVersion>> _versionRepoMock;
    private readonly Mock<IGenericRepository<PageRegion>> _pageRegionRepoMock;
    private readonly Mock<IGenericRepository<FileResource>> _fileResourceRepoMock;
    private readonly Mock<IGenericRepository<AuditEvent>> _auditRepoMock;
    private readonly Mock<IChapterPageAnnotationRepository> _annotationRepoMock;
    private readonly Mock<IChapterPageTaskRepository> _taskRepoMock;
    private readonly ChapterPageVersionService _service;

    public ChapterPageVersionServiceTests()
    {
        _versionRepoMock = new Mock<IGenericRepository<ChapterPageVersion>>();
        _pageRegionRepoMock = new Mock<IGenericRepository<PageRegion>>();
        _fileResourceRepoMock = new Mock<IGenericRepository<FileResource>>();
        _auditRepoMock = new Mock<IGenericRepository<AuditEvent>>();
        _annotationRepoMock = new Mock<IChapterPageAnnotationRepository>();
        _taskRepoMock = new Mock<IChapterPageTaskRepository>();

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.ChapterPageVersions).Returns(_versionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.PageRegions).Returns(_pageRegionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.FileResources).Returns(_fileResourceRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.AuditEvents).Returns(_auditRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.ChapterPageAnnotations).Returns(_annotationRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.ChapterPageTasks).Returns(_taskRepoMock.Object);

        _service = new ChapterPageVersionService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateChapterPageVersionAsync_CreatesAndReturnsDto()
    {
        var pageId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var dto = new CreateChapterPageVersionDto(pageId, 1, fileId, "First version");

        _versionRepoMock.Setup(r => r.AddAsync(It.IsAny<ChapterPageVersion>()))
            .Callback<ChapterPageVersion>(v => v.ChapterPageVersionId = Guid.NewGuid());

        var result = await _service.CreateChapterPageVersionAsync(dto);

        Assert.Equal(pageId, result.ChapterPageId);
        Assert.Equal(1, result.VersionNo);
        Assert.Equal(fileId, result.PageFileId);
        Assert.Equal("First version", result.VersionNote);
        _versionRepoMock.Verify(r => r.AddAsync(It.IsAny<ChapterPageVersion>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteChapterPageVersionAsync_Throws()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteChapterPageVersionAsync(Guid.NewGuid()));

        Assert.Contains("cannot be deleted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteVersionImageAsync_ReturnsNotSuccess_WhenActorEmpty()
    {
        var result = await _service.DeleteVersionImageAsync(Guid.NewGuid(), Guid.Empty, "Assistant");

        Assert.False(result.Success);
        Assert.NotNull(result.BlockedReason);
    }

    [Fact]
    public async Task DeleteVersionImageAsync_ReturnsNotSuccess_WhenVersionNotFound()
    {
        _versionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ChapterPageVersion?)null);

        var result = await _service.DeleteVersionImageAsync(Guid.NewGuid(), Guid.NewGuid(), "Assistant");

        Assert.False(result.Success);
        Assert.Contains("not found", result.BlockedReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteVersionImageAsync_Blocks_WhenUnresolvedAnnotations()
    {
        var versionId = Guid.NewGuid();
        var regionId = Guid.NewGuid();

        var version = new ChapterPageVersion
        {
            ChapterPageVersionId = versionId,
            ChapterPageId = Guid.NewGuid(),
            VersionNo = 1,
            PageFileId = Guid.NewGuid()
        };

        var regions = new List<PageRegion>
        {
            new() { PageRegionId = regionId, ChapterPageVersionId = versionId }
        };

        var annotations = new List<ChapterPageAnnotation>
        {
            new()
            {
                ChapterPageAnnotationId = Guid.NewGuid(),
                ResolvedAtUtc = null,
                PageRegions = new List<PageRegion> { regions[0] }
            }
        };

        _versionRepoMock.Setup(r => r.GetByIdAsync(versionId)).ReturnsAsync(version);
        _pageRegionRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PageRegion, bool>>>()))
            .ReturnsAsync(regions);
        _annotationRepoMock.Setup(r => r.GetByPageRegionIdsAsync(It.IsAny<IReadOnlyList<Guid>>()))
            .ReturnsAsync(annotations);

        var result = await _service.DeleteVersionImageAsync(versionId, Guid.NewGuid(), "Assistant");

        Assert.False(result.Success);
        Assert.Contains("unresolved annotations", result.BlockedReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteVersionImageAsync_SoftDeletes_WhenNoBlockers()
    {
        var versionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var chapterPageId = Guid.NewGuid();

        var version = new ChapterPageVersion
        {
            ChapterPageVersionId = versionId,
            ChapterPageId = chapterPageId,
            VersionNo = 1,
            PageFileId = fileId
        };

        var file = new FileResource
        {
            FileResourceId = fileId,
            CloudinaryPublicId = "test/public/id",
            DeletedAtUtc = null,
            DeletedByUserId = null
        };

        _versionRepoMock.Setup(r => r.GetByIdAsync(versionId)).ReturnsAsync(version);
        _pageRegionRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PageRegion, bool>>>()))
            .ReturnsAsync(new List<PageRegion>());
        _fileResourceRepoMock.Setup(r => r.GetByIdAsync(fileId)).ReturnsAsync(file);

        var result = await _service.DeleteVersionImageAsync(versionId, actorUserId, "Assistant");

        Assert.True(result.Success);
        Assert.Null(result.BlockedReason);
        Assert.Equal("test/public/id", result.CloudinaryPublicId);
        Assert.NotNull(file.DeletedAtUtc);
        Assert.Equal(actorUserId, file.DeletedByUserId);
    }
}
