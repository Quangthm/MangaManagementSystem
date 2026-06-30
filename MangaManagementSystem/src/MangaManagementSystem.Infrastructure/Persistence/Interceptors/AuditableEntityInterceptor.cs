using MangaManagementSystem.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Persistence.Interceptors
{
    public class AuditableEntityInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void UpdateEntities(DbContext? context)
        {
            if (context == null) return;

            foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    var utcNow = DateTime.UtcNow;
                    var createdProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAtUtc");
                    if (createdProp != null && (createdProp.CurrentValue == null || (DateTime)createdProp.CurrentValue == default(DateTime)))
                    {
                        createdProp.CurrentValue = utcNow;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    var utcNow = DateTime.UtcNow;
                    var updatedProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAtUtc");
                    if (updatedProp != null)
                    {
                        var updatedUserProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedByUserId");
                        if (updatedUserProp != null)
                        {
                            if (updatedUserProp.CurrentValue != null && (Guid)updatedUserProp.CurrentValue != Guid.Empty)
                            {
                                updatedProp.CurrentValue = utcNow;
                            }
                            else
                            {
                                var createdUserProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedByUserId");
                                if (createdUserProp?.CurrentValue != null && (Guid)createdUserProp.CurrentValue != Guid.Empty)
                                {
                                    updatedUserProp.CurrentValue = createdUserProp.CurrentValue;
                                    updatedProp.CurrentValue = utcNow;
                                }
                                else
                                {
                                    updatedProp.CurrentValue = null;
                                }
                            }
                        }
                        else
                        {
                            updatedProp.CurrentValue = utcNow;
                        }
                    }
                }
            }
        }
    }
}
