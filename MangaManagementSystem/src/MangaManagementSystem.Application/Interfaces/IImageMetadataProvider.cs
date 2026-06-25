namespace MangaManagementSystem.Application.Interfaces
{
    public sealed record ImageBoundsDto(int Width, int Height);

    public interface IImageMetadataProvider
    {
        Task<ImageBoundsDto?> GetImageBoundsAsync(
            string cloudinaryPublicId,
            CancellationToken cancellationToken);
    }
}
