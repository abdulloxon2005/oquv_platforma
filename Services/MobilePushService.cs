using System;
using System.Threading.Tasks;

namespace talim_platforma.Services
{
    public class MobilePushService : IMobilePushService
    {
        public async Task SendPushToUserAsync(string topicOrToken, string title, string body)
        {
            try
            {
                // Firebase orqali push xabar yuborish
                await FirebaseService.YuborishAsync(topicOrToken, title, body);
            }
            catch (Exception ex)
            {
                // Xatolarni log qilish (production'da ILogger ishlatish tavsiya etiladi)
                Console.WriteLine($"Push xabar yuborishda xato: {ex.Message}");
                // Xatoga qaramay, exception tashlamaymiz, chunki bu asosiy funksiyani buzmasligi kerak
            }
        }

        // Qo'shimcha metod - eski kod bilan moslashish uchun
        public async Task SendMobilePushAsync(string topicOrToken, string title, string body)
        {
            await SendPushToUserAsync(topicOrToken, title, body);
        }
    }
}
