using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using talim_platforma.Data;
using talim_platforma.Services;
using talim_platforma.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<BotOptions>(builder.Configuration.GetSection("Bot"));
builder.Services.AddScoped<IMobilePushService, MobilePushService>();
builder.Services.AddScoped<IStudentStatusService, StudentStatusService>();



builder.Services.AddSingleton<TelegramBotService>();
builder.Services.AddHostedService<BotHostedService>();
builder.Services.AddHostedService<MonthlyPaymentResetService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔒 Faqat web uchun Cookie autentifikatsiya
builder.Services.AddAuthentication(options =>
{
    // Default scheme Cookie bo'ladi (web uchun)
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddHttpClient();

// builder.WebHost.UseUrls("http://0.0.0.0:5024");
builder.WebHost.UseUrls("http://localhost:5025");
var app = builder.Build();

// 🔁 Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 🔧 Ensure database is migrated (creates Advertisements table, etc.)
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//     db.Database.Migrate();
// }

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
// 🔐 Auth middleware lar - to'g'ri tartibda joylashtirildi
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
