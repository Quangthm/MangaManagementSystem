using System;
using System.Linq;
using System.Threading.Tasks;
using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Common
{
    internal static class ProposalDecisionNotificationSupport
    {
        public static async Task AddForActiveMangakaContributorsAsync(
            IUnitOfWork unitOfWork,
            Guid seriesId,
            Guid seriesProposalId,
            string title,
            string message)
        {
            var contributors =
                await unitOfWork.SeriesContributors.FindAsync(
                    contributor =>
                        contributor.SeriesId == seriesId
                        && contributor.EndDate == null
                        && contributor.User != null
                        && contributor.User.StatusCode == "ACTIVE"
                        && contributor.User.Role != null
                        && contributor.User.Role.RoleName == "Mangaka");

            var recipientUserIds =
                contributors
                    .Select(contributor => contributor.UserId)
                    .Distinct()
                    .ToList();

            foreach (var recipientUserId in recipientUserIds)
            {
                await unitOfWork.Notifications.AddAsync(
                    new Notification
                    {
                        RecipientUserId = recipientUserId,
                        NotificationTypeCode =
                            NotificationTypeCodes.ProposalDecision,
                        Title = title,
                        Message = message,
                        RelatedEntityType =
                            NotificationRelatedEntityTypes.SeriesProposal,
                        RelatedEntityId = seriesProposalId,
                        CreatedAtUtc = DateTime.UtcNow
                    });
            }
        }
    }
}
