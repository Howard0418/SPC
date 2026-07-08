using System.Text;
using Scalar.AspNetCore;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.Services.Parsers;
using MesSpc.Api.Services.TestData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using MesSpc.Api.Services.Security;

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
    opt.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});
builder.Services.AddScoped<FormulaEngineService>();
builder.Services.AddScoped<SpcService>();
builder.Services.AddScoped<UploadService>();
builder.Services.AddScoped<ChemicalDailyReportParser>();
builder.Services.AddScoped<ChemicalImportService>();
builder.Services.AddScoped<IEmailNotificationService, SmtpEmailNotificationService>();
builder.Services.AddScoped<FakeDataFactory>();
builder.Services.AddScoped<SpcSampleGenerator>();
builder.Services.AddScoped<MeasurementGenerator>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<TestDataSeeder>();
builder.Services.AddScoped<GenealogyService>();
builder.Services.AddScoped<SpcOverviewReportService>();
builder.Services.AddSingleton<UserPasswordHasher>();
builder.Services.AddHostedService<MesSyncProcessorService>();
builder.Services.AddHostedService<SpcReportSchedulerService>();
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("dev", policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseCors("dev");

var frontendRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "frontend"));
if (!Directory.Exists(frontendRoot))
{
    frontendRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "frontend", "mes-spc-web", "dist"));
}

if (Directory.Exists(frontendRoot) && File.Exists(Path.Combine(frontendRoot, "index.html")))
{
    var frontendFiles = new PhysicalFileProvider(frontendRoot);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = frontendFiles });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = frontendFiles });
}

// ── 全域例外處理（Production & Development 均啟用）──
// 確保所有未處理例外都回傳 JSON 格式的錯誤，而非中斷連線
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        var feature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = feature?.Error;
        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = 500;

        // 外鍵違規 / 資料庫限制
        if (ex is Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            ctx.Response.StatusCode = 409;
            var inner = dbEx.InnerException?.Message ?? dbEx.Message;
            var msg = inner.Contains("FOREIGN KEY") || inner.Contains("REFERENCE")
                ? "此資料已被其他記錄關聯（外鍵約束），請先刪除或解除相關聯的資料後再操作。"
                : "資料庫更新失敗：" + inner;
            await ctx.Response.WriteAsJsonAsync(new { message = msg, detail = inner });
            return;
        }

        await ctx.Response.WriteAsJsonAsync(new
        {
            message = ex?.Message ?? "伺服器發生未預期的錯誤",
            detail  = ex?.InnerException?.Message
        });
    });
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<ViewerWriteGuardMiddleware>();
}

var controllers = app.MapControllers();
if (authEnabled)
{
    controllers.RequireAuthorization();
}

if (Directory.Exists(frontendRoot) && File.Exists(Path.Combine(frontendRoot, "index.html")))
{
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(frontendRoot)
    });
}

using (var scope = app.Services.CreateScope())
{
    var provider = builder.Configuration["DatabaseProvider"]?.Trim().ToLowerInvariant() ?? "sqlserver";
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (provider == "sqlite")
    {
        // SQLite is used for local rehearsal/test databases. This project does
        // not carry provider-specific SQLite migrations, so build a fresh
        // database directly from the current EF model.
        db.Database.EnsureCreated();
    }
    else
    {
        try { EnsureSqlServerMigrationBaseline(db); } catch { }
        db.Database.Migrate();
    }

    try
    {
        if (provider == "sqlite")
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS [ControlLimitSegments] (
                    [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    [PartProcessCharacteristicId] INTEGER NOT NULL,
                    [StartDate] TEXT NOT NULL,
                    [EndDate] TEXT NULL,
                    [UCL] REAL NULL,
                    [CL] REAL NULL,
                    [LCL] REAL NULL,
                    [Note] TEXT NULL,
                    [CreatedAt] TEXT NOT NULL,
                    [CreatedBy] TEXT NULL,
                    [UpdatedAt] TEXT NULL,
                    [UpdatedBy] TEXT NULL,
                    [IsDeleted] INTEGER NOT NULL DEFAULT 0,
                    [RowVersion] BLOB NULL,
                    CONSTRAINT [FK_ControlLimitSegments_PartProcessCharacteristics_PartProcessCharacteristicId] 
                        FOREIGN KEY ([PartProcessCharacteristicId]) REFERENCES [PartProcessCharacteristics] ([Id]) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS [IX_ControlLimitSegments_PartProcessCharacteristicId] ON [ControlLimitSegments] ([PartProcessCharacteristicId]);
            ");
        }
        else
        {
            db.Database.ExecuteSqlRaw(@"
                IF OBJECT_ID(N'[ControlLimitSegments]') IS NULL
                BEGIN
                    CREATE TABLE [ControlLimitSegments] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [PartProcessCharacteristicId] int NOT NULL,
                        [StartDate] datetime2 NOT NULL,
                        [EndDate] datetime2 NULL,
                        [UCL] float NULL,
                        [CL] float NULL,
                        [LCL] float NULL,
                        [Note] nvarchar(max) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(max) NULL,
                        [IsDeleted] bit NOT NULL DEFAULT 0,
                        [RowVersion] rowversion NULL,
                        CONSTRAINT [PK_ControlLimitSegments] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ControlLimitSegments_PartProcessCharacteristics_PartProcessCharacteristicId] 
                            FOREIGN KEY ([PartProcessCharacteristicId]) REFERENCES [PartProcessCharacteristics] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_ControlLimitSegments_PartProcessCharacteristicId] ON [ControlLimitSegments] ([PartProcessCharacteristicId]);
                END
            ");
        }
    }
    catch { }

    // Ensure GroupType column exists in ControlChartGroups
    try
    {
        if (provider == "sqlite")
        {
            try
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE [ControlChartGroups] ADD COLUMN [GroupType] TEXT NOT NULL DEFAULT 'CONTROL_CHART';");
            }
            catch { }
        }
        else
        {
            db.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (
                    SELECT 1 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[ControlChartGroups]') 
                      AND name = N'GroupType'
                )
                BEGIN
                    ALTER TABLE [ControlChartGroups] ADD [GroupType] nvarchar(max) NOT NULL DEFAULT 'CONTROL_CHART';
                END
            ");
        }
    }
    catch { }

    var seedDb = app.Configuration.GetValue<bool>("SeedDatabase", false);
    if (seedDb)
    {
        SeedData.Initialize(db);
    }
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
