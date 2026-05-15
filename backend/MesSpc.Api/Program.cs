using System.Text;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.Services.TestData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var authEnabled = builder.Configuration.GetValue("Auth:Enabled", false);
if (authEnabled)
{
    var jwtKey = builder.Configuration["Auth:JwtKey"];
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
        throw new InvalidOperationException("Auth:Enabled 為 true 時，必須設定 Auth:JwtKey（至少 32 字元）。");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.FromMinutes(2)
            };
        });
    builder.Services.AddAuthorization();
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var provider = builder.Configuration["DatabaseProvider"]?.Trim().ToLowerInvariant() ?? "sqlserver";
    if (provider == "sqlite")
    {
        var sqliteConn = builder.Configuration.GetConnectionString("Sqlite")
            ?? "Data Source=mes-spc.db";
        opt.UseSqlite(sqliteConn);
    }
    else
    {
        var sqlServerConn = builder.Configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("ConnectionStrings:SqlServer 未設定。");
        opt.UseSqlServer(sqlServerConn);
    }
});
builder.Services.AddScoped<FormulaEngineService>();
builder.Services.AddScoped<SpcService>();
builder.Services.AddScoped<UploadService>();
builder.Services.AddScoped<ChemicalDailyReportParser>();
builder.Services.AddScoped<ChemicalImportService>();
builder.Services.AddScoped<FakeDataFactory>();
builder.Services.AddScoped<SpcSampleGenerator>();
builder.Services.AddScoped<MeasurementGenerator>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<TestDataSeeder>();
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("dev", policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseCors("dev");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

var controllers = app.MapControllers();
if (authEnabled)
{
    controllers.RequireAuthorization();
}

using (var scope = app.Services.CreateScope())
{
    var provider = builder.Configuration["DatabaseProvider"]?.Trim().ToLowerInvariant() ?? "sqlserver";
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (provider == "sqlite")
    {
        db.Database.Migrate();
    }
    else
    {
        EnsureSqlServerMigrationBaseline(db);
        db.Database.Migrate();
    }
    SeedData.Initialize(db);
}

app.Run();

static void EnsureSqlServerMigrationBaseline(AppDbContext db)
{
    // 確保 __EFMigrationsHistory 表存在
    db.Database.ExecuteSqlRaw(@"
        IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
        BEGIN
            CREATE TABLE [__EFMigrationsHistory] (
                [MigrationId] nvarchar(150) NOT NULL,
                [ProductVersion] nvarchar(32) NOT NULL,
                CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
            );
        END");

    // 檢查是否已有資料表且尚未標記為已遷移
    var migrationIds = new[] 
    { 
        "20260507050816_InitialCreate", 
        "20260507085750_V2_Phase1_WorkOrder_StationOps_AlertWorkflow",
        "20260508040029_V3_MasterDataAndUploads",
        "20260513060320_InitialSqlServer" 
    };

    foreach (var id in migrationIds)
    {
        db.Database.ExecuteSqlRaw(@"
            IF OBJECT_ID(N'[Products]') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = {0})
            BEGIN
                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                VALUES ({0}, '10.0.7');
            END", id);
    }
}
