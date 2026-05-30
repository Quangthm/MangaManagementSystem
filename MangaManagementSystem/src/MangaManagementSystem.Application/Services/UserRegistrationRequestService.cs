using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class UserRegistrationRequestService : IUserRegistrationRequestService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserRegistrationRequestService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<UserRegistrationRequestDto> CreateUserRegistrationRequestAsync(CreateUserRegistrationRequestDto dto)
        {
            var entity = new UserRegistrationRequest
            {
                UserId = dto.UserId,
                RequestedRoleId = dto.RequestedRoleId,
                PortfolioFileId = dto.PortfolioFileId,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.UserRegistrationRequests.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<UserRegistrationRequestDto?> GetUserRegistrationRequestByIdAsync(long id)
        {
            var entity = await _unitOfWork.UserRegistrationRequests.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<UserRegistrationRequestDto>> GetAllUserRegistrationRequestsAsync()
        {
            var entities = await _unitOfWork.UserRegistrationRequests.GetAllAsync();
            return entities.Select(MapToDto);
        }

        public async Task<IEnumerable<UserRegistrationRequestDto>> GetUserRegistrationRequestsByStatusAsync(string status)
        {
            var all = await _unitOfWork.UserRegistrationRequests.GetAllAsync();
            return all.Where(r => r.Status == status).Select(MapToDto);
        }

        public async Task<UserRegistrationRequestDto?> UpdateUserRegistrationRequestStatusAsync(UpdateUserRegistrationRequestStatusDto dto)
        {
            var entity = await _unitOfWork.UserRegistrationRequests.GetByIdAsync(dto.RegistrationRequestId);
            if (entity == null)
            {
                return null;
            }

            entity.Status = dto.Status;
            entity.ReviewedByUserId = dto.ReviewedByUserId;
            _unitOfWork.UserRegistrationRequests.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        private static UserRegistrationRequestDto MapToDto(UserRegistrationRequest r) => new(
            r.RegistrationRequestId,
            r.UserId,
            r.RequestedRoleId,
            r.PortfolioFileId,
            r.Status,
            r.ReviewedByUserId
        );
    }
}
