using System.Threading.RateLimiting;
using FlightBooking.BUS;
using FlightBooking.DAO;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
// Giữ ưu tiên cấu hình ASP.NET (kể cả giá trị rỗng tường minh); chỉ fallback
// sang cấu hình MyConnection khi key chưa được cấu hình.
builder.Services.AddSingleton(new DatVeBUS(builder.Configuration.GetConnectionString("FlightBookingDB")
    ?? MyConnection.DocChuoiKetNoi() ?? ""));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromHours(1);
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("booking", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options => options.ValueCountLimit = 150);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("vi-VN").AddSupportedCultures("vi-VN").AddSupportedUICultures("vi-VN"));
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    await next();
});

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets().RequireRateLimiting("booking");

app.Run();
