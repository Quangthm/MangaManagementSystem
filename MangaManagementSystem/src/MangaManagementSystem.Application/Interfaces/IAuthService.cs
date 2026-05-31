using MangaManagementSystem.Application.DTOs.Auth;



namespace MangaManagementSystem.Application.Interfaces

{

    public interface IAuthService

    {

        Task<bool> SendRegistrationOtpAsync(RegisterDto request);

        Task<UserDto> CompleteRegistrationWithOtpAsync(string email, string otp);

        Task<AuthResultDto> LoginAsync(LoginDto request);

        Task<AuthResultDto> GetUserByEmailAsync(string email);

    }

}


