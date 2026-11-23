


using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using web.Models.Entities;
using web.Services;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. DATABASE
// ==========================================

builder.Services.AddDbContext<RetailFlowDBContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ==========================================
// 2. ĐĂNG KÝ CÁC SERVICE HIỆN TẠI
// ==========================================

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// ==========================================
// 3. PASSWORD HASHING
// ==========================================

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// ==========================================
// 4. USER SEEDER
// ==========================================

builder.Services.AddScoped<UserSeeder>();

// ==========================================
// 5. MODEL TRAINING
// ==========================================

builder.Services.AddScoped<IModelTrainingService, ModelTrainingService>();

// ==========================================
// 6. FORECAST
// ==========================================

builder.Services.AddScoped<IForecastService, ForecastService>();

// ==========================================
// 7. PYTHON FORECAST API
// ==========================================

builder.Services.Configure<ForecastApiOptions>(
    builder.Configuration.GetSection("ForecastApi")
);
builder.Services.Configure<ForecastApiOptions>(
    builder.Configuration.GetSection("PythonApi"));

builder.Services.AddHttpClient<
    IForecastApiClient,
    ForecastApiClient>();

builder.Services.AddScoped<
    IForecastService,
    ForecastService>();

builder.Services.AddHttpClient<
    IModelTrainingApiClient,
    ModelTrainingApiClient>();

builder.Services.AddScoped<
    IModelTrainingService,
    ModelTrainingService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("Email")
);
builder.Services.AddHttpClient<IForecastApiClient, ForecastApiClient>(
    (serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<ForecastApiOptions>>()
            .Value;

        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    }
);

// ==========================================
// 8. AUTHENTICATION
// ==========================================

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Cookie.Name = "RetailFlow.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// ==========================================
// 9. MVC
// ==========================================

builder.Services.AddControllersWithViews();

// ==========================================
// 10. BUILD APPLICATION
// ==========================================

var app = builder.Build();

// ==========================================
// 11. USER SEEDER
// ==========================================

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider
        .GetRequiredService<UserSeeder>();

    await seeder.SeedUsersAsync();
}

// ==========================================
// 12. MIDDLEWARE
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

