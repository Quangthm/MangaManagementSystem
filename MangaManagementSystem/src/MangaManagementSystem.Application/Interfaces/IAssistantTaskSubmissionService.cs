using MangaManagementSystem.Application.DTOs.Manga;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IAssistantTaskSubmissionService
    {
        Task<AssistantTaskSubmitResultDto> SubmitTaskWorkAsync(
            AssistantTaskSubmitRequestDto request,
            System.Threading.CancellationToken cancellationToken = default);
    }
}
