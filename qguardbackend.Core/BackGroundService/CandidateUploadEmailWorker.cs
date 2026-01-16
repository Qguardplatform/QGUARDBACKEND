using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using qguardbackend.BoilerPlate.Service.Interfaces;

namespace qguardbackend.Core.BackGroundService
{
    public class CandidateUploadEmailWorker : BackgroundService
    {
        private readonly IBackgroundEmailQueue _emailQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CandidateUploadEmailWorker> _logger;

        public CandidateUploadEmailWorker(
            IBackgroundEmailQueue emailQueue,
            IServiceProvider serviceProvider,
            ILogger<CandidateUploadEmailWorker> logger)
        {
            _emailQueue = emailQueue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CandidateUploadEmailWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var email = await _emailQueue.DequeueAsync(stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    await emailService.SendWelcomeEmailAsync(email);
                    _logger.LogInformation("Welcome email sent to {Receiver}", email.receiverEmail);
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing email.");
                }
            }

            _logger.LogInformation("CandidateUploadEmailWorker stopped.");
        }
    }
}