using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace talim_platforma.Services
{
    public class BotHostedService : BackgroundService
    {
        private readonly TelegramBotService _botService;

        public BotHostedService(TelegramBotService botService)
        {
            _botService = botService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _botService.StartReceiving(stoppingToken);
            // Servis to'xtatilmaguncha tirik turadi
            return Task.CompletedTask;
        }
    }
}
