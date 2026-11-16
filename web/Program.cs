

//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Options;
//using web.Models.Entities;
//using web.Services;

//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddDbContext<RetailFlowDBContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("DefaultConnection")
//    )
//);

//builder.Services.AddScoped<IProductService, ProductService>();
//builder.Services.AddScoped<ICategoryService, CategoryService>();
//builder.Services.AddScoped<IOrderService, OrderService>();
//builder.Services.AddScoped<IInventoryService, InventoryService>();

//builder.Services.AddScoped<IAccountService, AccountService>();
//builder.Services.AddScoped<IDashboardService, DashboardService>();

//// Password Hashing
//builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

//// User Seeder
//builder.Services.AddScoped<UserSeeder>();


//builder.Services
//    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(options =>
//    {
//        options.LoginPath = "/Account/Login";
//        options.AccessDeniedPath = "/Account/AccessDenied";

//        options.Cookie.Name = "RetailFlow.Auth";
//        options.Cookie.HttpOnly = true;
//        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

//        options.ExpireTimeSpan = TimeSpan.FromHours(8);
//        options.SlidingExpiration = true;
//    });

//builder.Services.AddAuthorization();


//builder.Services.AddControllersWithViews();

//var app = builder.Build();


//using (var scope = app.Services.CreateScope())
//{
//    var seeder = scope.ServiceProvider
//        .GetRequiredService<UserSeeder>();

//    await seeder.SeedUsersAsync();
//}

//// ModelTraining
//builder.Services.AddScoped<IModelTrainingService, ModelTrainingService>();

//// Forecast
//builder.Services.AddScoped<IForecastService, ForecastService>();

//// Python Forecast API
//builder.Services.Configure<ForecastApiOptions>(
//    builder.Configuration.GetSection("ForecastApi")
//);

//builder.Services.AddHttpClient<IForecastApiClient, ForecastApiClient>(
//    (serviceProvider, client) =>
//    {
//        var options = serviceProvider
//            .GetRequiredService<IOptions<ForecastApiOptions>>()
//            .Value;

//        client.BaseAddress = new Uri(options.BaseUrl);
//        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
//    }
//);
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();

//app.UseRouting();

//app.UseAuthentication();
//app.UseAuthorization();

//app.MapStaticAssets();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}")
//    .WithStaticAssets();

//app.Run();

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

