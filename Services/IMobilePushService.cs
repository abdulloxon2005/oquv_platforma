namespace talim_platforma.Services
{
    public interface IMobilePushService
    {
        Task SendPushToUserAsync(string topicOrToken, string title, string body);
    }
}
