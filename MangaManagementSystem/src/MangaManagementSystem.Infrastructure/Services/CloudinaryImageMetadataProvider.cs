using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Infrastructure.Services
{
    public class CloudinaryImageMetadataProvider : IImageMetadataProvider
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryImageMetadataProvider> _logger;

        public CloudinaryImageMetadataProvider(Cloudinary cloudinary, ILogger<CloudinaryImageMetadataProvider> logger)
        {
            _cloudinary = cloudinary ?? throw new ArgumentNullException(nameof(cloudinary));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ImageBoundsDto?> GetImageBoundsAsync(
            string cloudinaryPublicId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(cloudinaryPublicId))
                return null;

            try
            {
                var getParams = new GetResourceParams(cloudinaryPublicId)
                {
                    ResourceType = ResourceType.Image
                };

                var result = await _cloudinary.GetResourceAsync(getParams);

                if (result == null || result.Width <= 0 || result.Height <= 0)
                {
                    _logger.LogWarning(
                        "Cloudinary resource {PublicId} returned no usable dimensions. Width={Width}, Height={Height}",
                        cloudinaryPublicId, result?.Width, result?.Height);
                    return null;
                }

                return new ImageBoundsDto(result.Width, result.Height);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to retrieve Cloudinary resource metadata for {PublicId}",
                    cloudinaryPublicId);
                return null;
            }
        }
    }
}
