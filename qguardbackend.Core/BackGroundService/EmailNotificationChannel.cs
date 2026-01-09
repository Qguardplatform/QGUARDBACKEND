using qguardbackend.Data.DTOs;
using System.Threading.Channels;

namespace qguardbackend.Core.BackGroundService
{
    public class EmailNotificationChannel
    {
        private readonly Channel<NotificationTask> _channel = Channel.CreateUnbounded<NotificationTask>();

        public async Task QueueNotificationAsync(NotificationTask task)
        {
            await _channel.Writer.WriteAsync(task);
        }

        public ChannelReader<NotificationTask> Reader => _channel.Reader;
    }
}
