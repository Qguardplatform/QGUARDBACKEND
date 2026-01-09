using qguardbackend.Data.DTOs.EmailDtos;

namespace qguardbackend.Core.BackGroundService
{
    public interface IBackgroundEmailQueue
    {
        void QueueEmail(SendWelcomeEmailVM email);
        Task<SendWelcomeEmailVM> DequeueAsync(CancellationToken cancellationToken);
    }
}