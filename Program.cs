using MedSafe.Web.Data;
using MedSafe.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- MVC ----------
builder.Services.AddControllersWithViews();

// ---------- request-scoped services ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<PdfReportService>();

// ---------- database (SQLite file, persists across restarts) ----------
var connectionString = builder.Configuration.GetConnectionString("MedSafeDb")
                       ?? "Data Source=medsafe.db";

builder.Services.AddDbContext<MedSafeDbContext>(options => options.UseSqlite(connectionString));

// ---------- session (keeps the signed-in account) ----------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.Name = "MedSafe.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCodePage", "?code={0}");
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ---------- create and seed the database on first run ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MedSafeDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

    if (db.Database.EnsureCreated())
    {
        DbSeeder.Seed(db, env);
    }
}

app.Run();
