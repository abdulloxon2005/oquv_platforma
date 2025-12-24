using System;
using System.IO;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace talim_platforma.Services
{
    public static class FirebaseService
    {
        static FirebaseService()
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                var firebaseKeyPath = "firebase-key.json";
                if (!File.Exists(firebaseKeyPath))
                {
                    Console.WriteLine($"⚠️ Ogohlantirish: Firebase kalit fayli topilmadi: {firebaseKeyPath}");
                    Console.WriteLine("⚠️ Push xabarlar ishlamaydi. Firebase kalit faylini loyiha ildiziga qo'ying.");
                    return;
                }

                try
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(firebaseKeyPath)
                    });
                    Console.WriteLine("✅ Firebase muvaffaqiyatli ishga tushirildi.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Firebase ishga tushirishda xatolik: {ex.Message}");
                }
            }
        }

        public static async Task YuborishAsync(string token, string title, string body)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                Console.WriteLine("⚠️ Firebase ishga tushmagan. Push xabar yuborilmadi.");
                return;
            }

            try
            {
                var message = new Message
                {
                    Token = token,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    }
                };
                await FirebaseMessaging.DefaultInstance.SendAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Push xabar yuborishda xatolik: {ex.Message}");
                throw; // Yuqoridagi catch blokiga tashlash uchun
            }
        }
    }
}
