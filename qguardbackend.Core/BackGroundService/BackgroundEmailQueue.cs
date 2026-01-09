using qguardbackend.Data.DTOs.EmailDtos;
using System.Threading.Channels;

namespace qguardbackend.Core.BackGroundService
{
    public class BackgroundEmailQueue : IBackgroundEmailQueue
    {
        private readonly Channel<SendWelcomeEmailVM> _queue =
            Channel.CreateUnbounded<SendWelcomeEmailVM>();

        public void QueueEmail(SendWelcomeEmailVM email) =>
            _queue.Writer.TryWrite(email);

        public async Task<SendWelcomeEmailVM> DequeueAsync(CancellationToken cancellationToken) =>
            await _queue.Reader.ReadAsync(cancellationToken);
    }
}