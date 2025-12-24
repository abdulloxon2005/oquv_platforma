using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using talim_platforma.Models;

namespace talim_platforma.Services
{
    public class TelegramBotService
    {
        private readonly TelegramBotClient _bot;
        private readonly long _adminChatId;

        public TelegramBotService(IOptions<BotOptions> options)
        {
            var cfg = options.Value;
            _bot = new TelegramBotClient(cfg.Token);
            _adminChatId = cfg.AdminChatId;
        }

        // 📡 Pollingni ishga tushirish
        public void StartReceiving(CancellationToken ct)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                ct
            );

            Console.WriteLine("🤖 Telegram bot polling boshlandi...");
        }

        // 📩 Foydalanuvchi xabarlarini qayta ishlash
        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text is null)
                return;

            var msg = update.Message;
            var text = msg.Text.Trim();

            if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
            {
                var firstName = msg.From?.FirstName ?? "";
                var lastName = msg.From?.LastName ?? "";
                string fullName = $"{firstName} {lastName}".Trim();

                string username = !string.IsNullOrEmpty(msg.From?.Username)
                    ? "@" + msg.From.Username
                    : fullName;

                long userChatId = msg.Chat.Id;

                // 📨 Admin uchun ma'lumot
                string adminInfo =
                    $"🆕 <b>Yangi foydalanuvchi</b>\n" +
                    $"👤 Ismi: {EscapeHtml(fullName)}\n" +
                    $"📛 Username: {EscapeHtml(username)}\n" +
                    $"🆔 Chat ID: <code>{userChatId}</code>";

                await _bot.SendTextMessageAsync(
                    chatId: _adminChatId,
                    text: adminInfo,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct
                );

                // 🎉 Foydalanuvchi uchun xabar
                string userMessage =
                    $"🎉 <b>Tabriklaymiz!</b>\n" +
                    $"Siz <b>Ta'lim Plus</b> o‘quv markazining rasmiy Telegram botiga muvaffaqiyatli ulandingiz.\n\n" +
                    $"📚 Endilikda sizga to‘lovlar, jadval va yangiliklar haqida xabarlar yuboriladi.\n\n" +
                    $"👨‍🎓 Ismingiz: <b>{EscapeHtml(fullName)}</b>\n\n" +
                    $"🤝 Biz bilan birga bo‘lganingizdan xursandmiz!";

                await _bot.SendTextMessageAsync(
                    chatId: userChatId,
                    text: userMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct
                );
            }
        }

        // ⚠️ Xatolikni konsolga chiqarish
        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"⚠️ Telegram polling xatosi: {ex.Message}");
            return Task.CompletedTask;
        }

        // 📨 Oddiy xabar yuborish (HTML formatda)
        public Task SendMessageAsync(long chatId, string message)
        {
            return _bot.SendTextMessageAsync(
                chatId: chatId,
                text: EscapeHtml(message),
                parseMode: ParseMode.Html
            );
        }

        // 💰 To‘lov amalga oshirilganda xabar yuborish
        public async Task SendPaymentNotificationAsync(
            long chatId,
            string studentName,
            string courseName,
            decimal amount,
            DateTime date,
            string status)
        {
            if (chatId == 0)
            {
                Console.WriteLine("⚠️ Telegram chat ID topilmadi. Xabar yuborilmadi.");
                return;
            }

            string msg =
                $"✅ <b>To‘lov amalga oshirildi!</b>\n\n" +
                $"👤 Talaba: <b>{EscapeHtml(studentName)}</b>\n" +
                $"📘 Kurs: <b>{EscapeHtml(courseName)}</b>\n" +
                $"💵 To‘lov summasi: <b>{amount:N0} so‘m</b>\n" +
                $"📅 Sana: <b>{date:dd.MM.yyyy HH:mm}</b>\n" +
                $"📊 Holat: <b>{EscapeHtml(status)}</b>\n\n" +
                $"Rahmat! Sizning to‘lovingiz muvaffaqiyatli qabul qilindi.";

            await _bot.SendTextMessageAsync(
                chatId: chatId,
                text: msg,
                parseMode: ParseMode.Html
            );
        }

        // 🔔 Mobil ilovaga push yuborish (hozircha log sifatida)
        public async Task SendMobilePushAsync(string deviceToken, string title, string message)
        {
            Console.WriteLine($"📱 Mobil push yuborildi -> {title}: {message}");
            await Task.CompletedTask;
        }

        // 🧹 HTML belgilarini tozalovchi funksiya
        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
    }
}
