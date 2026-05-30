using MangaManagementSystem.Application.DTOs.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IUserRegistrationRequestService
    {
        Task<UserRegistrationRequestDto> CreateUserRegistrationRequestAsync(CreateUserRegistrationRequestDto dto);
        Task<UserRegistrationRequestDto?> GetUserRegistrationRequestByIdAsync(long id);
        Task<IEnumerable<UserRegistrationRequestDto>> GetAllUserRegistrationRequestsAsync();
        Task<IEnumerable<UserRegistrationRequestDto>> GetUserRegistrationRequestsByStatusAsync(string status);
        Task<UserRegistrationRequestDto?> UpdateUserRegistrationRequestStatusAsync(UpdateUserRegistrationRequestStatusDto dto);
    }
}
