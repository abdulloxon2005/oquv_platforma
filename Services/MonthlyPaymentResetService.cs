using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using talim_platforma.Data;
using talim_platforma.Helpers;

namespace talim_platforma.Services
{
    /// <summary>
    /// Har oyning 1-sanasi da to'lovlarni yangilash va 1-3 sanalar oralig'ida ertalab 8:00 da xabar yuborish uchun servis
    /// </summary>
    public class MonthlyPaymentResetService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonthlyPaymentResetService> _logger;
        private readonly TelegramBotService _telegramBot;

        public MonthlyPaymentResetService(
            IServiceProvider serviceProvider,
            ILogger<MonthlyPaymentResetService> logger,
            TelegramBotService telegramBot)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _telegramBot = telegramBot;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔄 MonthlyPaymentResetService ishga tushdi.");

            // Birinchi marta ishga tushganda, keyingi 1-sana va 8:00 ni hisoblaymiz
            DateTime? lastResetDate = null;
            DateTime? lastNotificationDate = null;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var currentDay = now.Day;
                    var currentHour = now.Hour;
                    var currentDate = now.Date;

                    // Har oyning 1-sanasi, ertalab soat 00:00 da to'lovlarni yangilash
                    if (currentDay == 1 && currentHour == 0 && lastResetDate != currentDate)
                    {
                        _logger.LogInformation("📅 Oy boshlandi. To'lovlarni yangilash boshlandi...");
                        await ResetMonthlyPaymentsAsync();
                        lastResetDate = currentDate;
                    }

                    // 1-3 sanalar oralig'ida ertalab soat 8:00 da xabar yuborish
                    if (currentDay >= 1 && currentDay <= 3 && currentHour == 8 && lastNotificationDate != currentDate)
                    {
                        _logger.LogInformation("📨 Oylik to'lov xabarlarini yuborish boshlandi...");
                        await SendMonthlyPaymentNotificationsAsync();
                        lastNotificationDate = currentDate;
                    }

                    // Har 30 minut tekshirish (aniq vaqtda ishlashi uchun)
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
                catch (Exception ex)
                {
                    // Faqat xatolikni log qilish, lekin logger disposed bo'lsa xatolikdan qochish
                    try
                    {
                        _logger.LogError(ex, "❌ MonthlyPaymentResetService xatosi: {message}", ex.Message);
                    }
                    catch
                    {
                        // Logger disposed bo'lsa, hech narsa qilmaymiz
                    }
                    
                    // Xato bo'lsa ham kutishda davom etish, lekin stoppingToken ni tekshirish
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Application to'xtatilganda, shunchaki chiqamiz
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Har oyning 1-sanasi da barcha talabalarning to'lovlarini yangi oy uchun yangilash
        /// </summary>
        private async Task ResetMonthlyPaymentsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
                var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

                // Barcha aktiv talabalarni olish (arxivlanganlar emas)
                var talabalar = await context.Foydalanuvchilar
                    .Where(f => RoleHelper.IsStudent(f.Rol) && f.Faolmi && !f.ArxivlanganSana.HasValue)
                    .ToListAsync();

                _logger.LogInformation($"📊 {talabalar.Count} ta talaba topildi.");

                foreach (var talaba in talabalar)
                {
                    // Talabaning aktiv kurslarini olish
                    var talabaKurslar = await context.TalabaKurslar
                        .Include(tk => tk.Kurs)
                        .Where(tk => tk.TalabaId == talaba.Id && tk.Aktivmi)
                        .ToListAsync();

                    foreach (var talabaKurs in talabaKurslar)
                    {
                        var kurs = talabaKurs.Kurs;
                        if (kurs == null) continue;

                        // O'tgan oyning oxiridagi holatni olish (barcha to'lovlardan keyin)
                        // Eng so'nggi to'lovni olish (qaysi oyda bo'lishidan qat'iy nazar)
                        var engSonggiTolov = await context.Tolovlar
                            .Where(t => t.TalabaId == talaba.Id && t.KursId == kurs.Id)
                            .OrderByDescending(t => t.Sana)
                            .FirstOrDefaultAsync();

                        // O'tgan oyning oxiridagi holat
                        decimal oyOxiridagiQarzdorlik = 0;
                        decimal oyOxiridagiHaqdorlik = 0;

                        if (engSonggiTolov != null)
                        {
                            // Eng so'nggi to'lovning holatini olish
                            oyOxiridagiQarzdorlik = engSonggiTolov.Qarzdorlik;
                            oyOxiridagiHaqdorlik = engSonggiTolov.Haqdorlik;
                        }
                        else
                        {
                            // Agar hech qachon to'lov bo'lmasa, kurs narxi qarzdorlik
                            oyOxiridagiQarzdorlik = kurs.Narxi;
                        }

                        // Yangi oy uchun to'lov holatini hisoblash
                        decimal yangiOyQarzdorlik = 0;
                        decimal yangiOyHaqdorlik = 0;
                        string yangiOyHolat = "";

                        if (oyOxiridagiHaqdorlik > 0)
                        {
                            // Agar haqdor bo'lsa, haqdorlik summasini yangi oy uchun to'lov sifatida qo'shamiz
                            // Lekin yangi oy uchun yana kurs narxi to'lanishi kerak
                            yangiOyQarzdorlik = kurs.Narxi - oyOxiridagiHaqdorlik;
                            if (yangiOyQarzdorlik < 0)
                            {
                                yangiOyHaqdorlik = Math.Abs(yangiOyQarzdorlik);
                                yangiOyQarzdorlik = 0;
                                yangiOyHolat = $"Haqdor ({yangiOyHaqdorlik:N0} so'm)";
                            }
                            else if (yangiOyQarzdorlik == 0)
                            {
                                yangiOyHolat = "To'liq to'langan";
                            }
                            else
                            {
                                yangiOyHolat = $"Qarzdor ({yangiOyQarzdorlik:N0} so'm)";
                            }
                        }
                        else if (oyOxiridagiQarzdorlik > 0)
                        {
                            // Agar qarzdor bo'lsa, qarzdorlik qo'shiladi
                            yangiOyQarzdorlik = oyOxiridagiQarzdorlik + kurs.Narxi;
                            yangiOyHolat = $"Qarzdor ({yangiOyQarzdorlik:N0} so'm)";
                        }
                        else
                        {
                            // Agar to'liq to'langan bo'lsa, yangi oy uchun qarzdor qilinadi
                            yangiOyQarzdorlik = kurs.Narxi;
                            yangiOyHolat = $"Qarzdor ({yangiOyQarzdorlik:N0} so'm)";
                        }

                        // TalabaKurs holatini yangilash
                        talabaKurs.Holati = yangiOyHolat;
                        talabaKurs.YangilanganVaqt = DateTime.Now;

                        _logger.LogInformation(
                            $"✅ Talaba: {talaba.Familiya} {talaba.Ism}, Kurs: {kurs.Nomi}, " +
                            $"Yangi oy holati: {yangiOyHolat}");
                    }
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("✅ Barcha to'lovlar muvaffaqiyatli yangilandi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ To'lovlarni yangilashda xatolik yuz berdi.");
                throw;
            }
        }

        /// <summary>
        /// 1-3 sanalar oralig'ida ertalab soat 8:00 da barcha talabalarga to'lov holati haqida xabar yuborish
        /// </summary>
        private async Task SendMonthlyPaymentNotificationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var currentDate = DateTime.Now.Date;
                var currentDay = currentDate.Day;

                // Faqat 1-3 sanalar oralig'ida ishlaydi
                if (currentDay < 1 || currentDay > 3)
                    return;

                // Barcha aktiv talabalarni olish (arxivlanganlar emas)
                var talabalar = await context.Foydalanuvchilar
                    .Where(f => RoleHelper.IsStudent(f.Rol) && f.Faolmi && !f.ArxivlanganSana.HasValue && !string.IsNullOrEmpty(f.ChatId))
                    .ToListAsync();

                _logger.LogInformation($"📨 {talabalar.Count} ta talabaga xabar yuborilmoqda...");

                foreach (var talaba in talabalar)
                {
                    try
                    {
                        // Talabaning aktiv kurslarini olish
                        var talabaKurslar = await context.TalabaKurslar
                            .Include(tk => tk.Kurs)
                            .Where(tk => tk.TalabaId == talaba.Id && tk.Aktivmi)
                            .ToListAsync();

                        if (!talabaKurslar.Any())
                            continue;

                        var xabarQismlari = new System.Collections.Generic.List<string>();
                        xabarQismlari.Add($"📅 <b>Oylik to'lov xabari</b>\n");
                        xabarQismlari.Add($"👤 <b>{talaba.Familiya} {talaba.Ism}</b>\n");

                        bool qarzdorBor = false;
                        decimal umumiyQarzdorlik = 0;

                        foreach (var talabaKurs in talabaKurslar)
                        {
                            var kurs = talabaKurs.Kurs;
                            if (kurs == null) continue;

                            var holat = talabaKurs.Holati ?? "Ma'lumot yo'q";
                            
                            // Qarzdorlikni ajratib olish
                            decimal qarzdorlik = 0;
                            if (holat.Contains("Qarzdor"))
                            {
                                var qarzdorlikMatch = System.Text.RegularExpressions.Regex.Match(holat, @"(\d+(?:\.\d+)?)");
                                if (qarzdorlikMatch.Success)
                                {
                                    decimal.TryParse(qarzdorlikMatch.Value, out qarzdorlik);
                                    umumiyQarzdorlik += qarzdorlik;
                                    qarzdorBor = true;
                                }
                            }

                            xabarQismlari.Add($"📘 <b>{kurs.Nomi}</b>");
                            xabarQismlari.Add($"   💰 Kurs narxi: {kurs.Narxi:N0} so'm");
                            xabarQismlari.Add($"   📊 Holat: {holat}\n");
                        }

                        if (qarzdorBor)
                        {
                            xabarQismlari.Add($"⚠️ <b>Umumiy qarzdorlik: {umumiyQarzdorlik:N0} so'm</b>\n");
                            xabarQismlari.Add("Iltimos, to'lovni o'z vaqtida amalga oshiring.");
                        }
                        else
                        {
                            xabarQismlari.Add("✅ Barcha kurslar uchun to'lovlar to'liq amalga oshirilgan.");
                        }

                        var xabar = string.Join("\n", xabarQismlari);

                        // Telegram xabarini yuborish
                        if (long.TryParse(talaba.ChatId, out long chatId))
                        {
                            await _telegramBot.SendHtmlMessageAsync(chatId, xabar);
                            _logger.LogInformation($"✅ Xabar yuborildi: {talaba.Familiya} {talaba.Ism} (ChatId: {chatId})");
                            
                            // Har bir xabar o'rtasida kichik kutish (rate limit uchun)
                            await Task.Delay(TimeSpan.FromMilliseconds(500));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"❌ Talaba {talaba.Familiya} {talaba.Ism} ga xabar yuborishda xatolik");
                        // Bitta talabaga xabar yuborishda xatolik bo'lsa, davom etamiz
                    }
                }

                _logger.LogInformation("✅ Barcha xabarlar yuborildi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Xabarlarni yuborishda xatolik yuz berdi.");
                throw;
            }
        }
    }
}

