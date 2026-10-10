using Microsoft.AspNetCore.DataProtection;
using MesSpc.Api.Services.Calibration;
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
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ChameleonStatusService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient();
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
builder.Services.AddScoped<ChemicalFTableService>();
builder.Services.AddScoped<ChemicalAnalysisFormulaVersionService>();
builder.Services.AddScoped<SpcService>();
builder.Services.AddScoped<UploadService>();
builder.Services.AddScoped<ParticleUploadService>();
builder.Services.AddScoped<ParticleQueryService>();
builder.Services.AddScoped<EtchReportSyncService>();
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
builder.Services.AddScoped<MesSyncMessageBatchProcessor>();
builder.Services.AddSingleton<UserPasswordHasher>();
builder.Services.AddHostedService<MesSyncProcessorService>();
builder.Services.AddHostedService<SpcReportSchedulerService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<MesSpc.Api.Services.Calibration.InstrumentCalibrationService>();
builder.Services.AddScoped<MesSpc.Api.Services.Calibration.CalibrationImportService>();
builder.Services.AddScoped<MesSpc.Api.Services.Calibration.CalibrationCertificates>();
builder.Services.AddScoped<MesSpc.Api.Services.Calibration.CalibrationNotificationProcessor>();
builder.Services.AddScoped<MesSpc.Api.Services.Calibration.ICalibrationMailSender, MesSpc.Api.Services.Calibration.CalibrationSmtpSender>();
builder.Services.AddSingleton<CalibrationChatSecrets>(_ =>
{
    var keyRoot = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "private-data", "data-protection-keys"));
    var provider = DataProtectionProvider.Create(keyRoot, options =>
    {
        options.SetApplicationName("SPC.Calibration.Chat");
        if (OperatingSystem.IsWindows()) options.ProtectKeysWithDpapi(protectToLocalMachine: true);
    });
    return new CalibrationChatSecrets(provider);
});
builder.Services.AddSingleton<ICalibrationChatSender>(services =>
    new CalibrationChatSender(
        new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(30), MaxResponseContentBufferSize = 65536 },
        services.GetRequiredService<ILogger<CalibrationChatSender>>()));
builder.Services.AddHostedService<MesSpc.Api.Services.Calibration.CalibrationNotificationSchedulerService>();
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

        if (ex is Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            ctx.Response.StatusCode = 409;
        }

        var includeDetails = app.Environment.IsDevelopment();
        await ctx.Response.WriteAsJsonAsync(ApiErrorResponseFactory.FromException(ex, includeDetails));
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

app.MapGet("/api/version", (IConfiguration configuration) => Results.Ok(new
{
    version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "unknown",
    environment = configuration["AppEnvironment"] ?? "unknown"
})).AllowAnonymous();

app.MapGet("/api/health", (IConfiguration configuration) => Results.Ok(new
{
    status = "ok",
    version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "unknown",
    environment = configuration["AppEnvironment"] ?? "unknown"
})).AllowAnonymous();

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
                    [USL] REAL NULL,
                    [LSL] REAL NULL,
                    [TargetValue] REAL NULL,
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
            db.Database.ExecuteSqlRaw("ALTER TABLE [ControlLimitSegments] ADD COLUMN [USL] REAL NULL;");
            db.Database.ExecuteSqlRaw("ALTER TABLE [ControlLimitSegments] ADD COLUMN [LSL] REAL NULL;");
            db.Database.ExecuteSqlRaw("ALTER TABLE [ControlLimitSegments] ADD COLUMN [TargetValue] REAL NULL;");
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
                        [USL] float NULL,
                        [LSL] float NULL,
                        [TargetValue] float NULL,
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
                IF COL_LENGTH('ControlLimitSegments', 'USL') IS NULL ALTER TABLE [ControlLimitSegments] ADD [USL] float NULL;
                IF COL_LENGTH('ControlLimitSegments', 'LSL') IS NULL ALTER TABLE [ControlLimitSegments] ADD [LSL] float NULL;
                IF COL_LENGTH('ControlLimitSegments', 'TargetValue') IS NULL ALTER TABLE [ControlLimitSegments] ADD [TargetValue] float NULL;
            ");
        }
    }
    catch { }

    try
    {
        if (provider != "sqlite")
        {
            db.Database.ExecuteSqlRaw(@"
                IF OBJECT_ID(N'[ChemicalFTableVersions]') IS NULL
                BEGIN
                    CREATE TABLE [ChemicalFTableVersions] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [VersionCode] nvarchar(50) NOT NULL,
                        [DisplayName] nvarchar(100) NOT NULL,
                        [SourceName] nvarchar(100) NULL,
                        [SourcePath] nvarchar(500) NULL,
                        [EffectiveAt] datetime2 NOT NULL,
                        [ImportedAt] datetime2 NOT NULL,
                        [IsActive] bit NOT NULL,
                        [Note] nvarchar(max) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(max) NULL,
                        [IsDeleted] bit NOT NULL DEFAULT 0,
                        [RowVersion] rowversion NULL,
                        CONSTRAINT [PK_ChemicalFTableVersions] PRIMARY KEY ([Id])
                    );
                    CREATE UNIQUE INDEX [IX_ChemicalFTableVersions_VersionCode] ON [ChemicalFTableVersions] ([VersionCode]);
                END

                IF OBJECT_ID(N'[ChemicalFTableCells]') IS NULL
                BEGIN
                    CREATE TABLE [ChemicalFTableCells] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [VersionId] int NOT NULL,
                        [SheetName] nvarchar(50) NOT NULL,
                        [CellAddress] nvarchar(50) NOT NULL,
                        [NormalizedCellAddress] nvarchar(50) NOT NULL,
                        [StandardSolution] nvarchar(100) NULL,
                        [NumericValue] decimal(18,6) NULL,
                        [TextValue] nvarchar(max) NULL,
                        [FormulaText] nvarchar(max) NULL,
                        [RawValue] nvarchar(max) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(max) NULL,
                        [IsDeleted] bit NOT NULL DEFAULT 0,
                        [RowVersion] rowversion NULL,
                        CONSTRAINT [PK_ChemicalFTableCells] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ChemicalFTableCells_ChemicalFTableVersions_VersionId]
                            FOREIGN KEY ([VersionId]) REFERENCES [ChemicalFTableVersions] ([Id]) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX [IX_ChemicalFTableCells_VersionId_NormalizedCellAddress]
                        ON [ChemicalFTableCells] ([VersionId], [NormalizedCellAddress]);
                END

                IF OBJECT_ID(N'[ChemicalFTableReferences]') IS NULL
                BEGIN
                    CREATE TABLE [ChemicalFTableReferences] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [VersionId] int NOT NULL,
                        [PartProcessCharacteristicId] int NOT NULL,
                        [CellAddress] nvarchar(50) NOT NULL,
                        [NormalizedCellAddress] nvarchar(50) NOT NULL,
                        [SourceFormula] nvarchar(max) NULL,
                        [SourceSheet] nvarchar(100) NULL,
                        [SourceRow] int NULL,
                        [ReferenceContext] nvarchar(200) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(max) NULL,
                        [IsDeleted] bit NOT NULL DEFAULT 0,
                        [RowVersion] rowversion NULL,
                        CONSTRAINT [PK_ChemicalFTableReferences] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ChemicalFTableReferences_ChemicalFTableVersions_VersionId]
                            FOREIGN KEY ([VersionId]) REFERENCES [ChemicalFTableVersions] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_ChemicalFTableReferences_PartProcessCharacteristics_PartProcessCharacteristicId]
                            FOREIGN KEY ([PartProcessCharacteristicId]) REFERENCES [PartProcessCharacteristics] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_ChemicalFTableReferences_PartProcessCharacteristicId]
                        ON [ChemicalFTableReferences] ([PartProcessCharacteristicId]);
                    CREATE INDEX [IX_ChemicalFTableReferences_VersionId_NormalizedCellAddress]
                        ON [ChemicalFTableReferences] ([VersionId], [NormalizedCellAddress]);
                    CREATE UNIQUE INDEX [IX_ChemicalFTableReferences_VersionId_PartProcessCharacteristicId_NormalizedCellAddress]
                        ON [ChemicalFTableReferences] ([VersionId], [PartProcessCharacteristicId], [NormalizedCellAddress]);
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

    // ──────────────── 自我修復 (Self-Healing) 機制：針對 MergeGroupsAndCategories 進行 SQL Server 資料庫相容遷移 ────────────────
    try
    {
        // 1. 若 ControlChartTypes 表中不存在 ChartGroupId 欄位則新增它
        db.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (
                SELECT 1 FROM sys.columns 
                WHERE object_id = OBJECT_ID(N'[ControlChartTypes]') AND name = N'ChartGroupId'
            )
            BEGIN
                ALTER TABLE [ControlChartTypes] ADD [ChartGroupId] INT NULL;
            END
        ");

        // 2. 當 ControlChartCategories 舊資料表仍存在時，將 Group 關聯 ID 複製到 ControlChartTypes
        db.Database.ExecuteSqlRaw(@"
            IF OBJECT_ID(N'[ControlChartCategories]') IS NOT NULL
               AND EXISTS (
                   SELECT 1 FROM sys.columns 
                   WHERE object_id = OBJECT_ID(N'[ControlChartTypes]') AND name = N'ChartGroupId'
               )
            BEGIN
                EXEC('
                    UPDATE ControlChartTypes
                    SET ChartGroupId = (
                        SELECT ChartGroupId 
                        FROM ControlChartCategories 
                        WHERE ControlChartCategories.Id = ControlChartTypes.ChartCategoryId
                    )
                    WHERE ChartGroupId IS NULL
                ');
            END
        ");

        // 3. 找出並移除 ControlChartTypes 與 ControlChartCategories 之間舊的外鍵約束
        db.Database.ExecuteSqlRaw(@"
            DECLARE @ConstraintName nvarchar(200)
            SELECT @ConstraintName = name 
            FROM sys.foreign_keys 
            WHERE parent_object_id = OBJECT_ID(N'[ControlChartTypes]') 
              AND referenced_object_id = OBJECT_ID(N'[ControlChartCategories]')
            IF @ConstraintName IS NOT NULL
            BEGIN
                EXEC('ALTER TABLE [ControlChartTypes] DROP CONSTRAINT [' + @ConstraintName + ']')
            END
        ");

        // 4. 移除 ChartCategoryId 欄位上的索引
        db.Database.ExecuteSqlRaw(@"
            IF EXISTS (
                SELECT 1 FROM sys.indexes 
                WHERE name = N'IX_ControlChartTypes_ChartCategoryId' AND object_id = OBJECT_ID(N'[ControlChartTypes]')
            )
            BEGIN
                DROP INDEX [IX_ControlChartTypes_ChartCategoryId] ON [ControlChartTypes];
            END
        ");

        // 5. 移除 ControlChartTypes 中的 ChartCategoryId 舊欄位
        db.Database.ExecuteSqlRaw(@"
            IF EXISTS (
                SELECT 1 FROM sys.columns 
                WHERE object_id = OBJECT_ID(N'[ControlChartTypes]') AND name = N'ChartCategoryId'
            )
            BEGIN
                ALTER TABLE [ControlChartTypes] DROP COLUMN [ChartCategoryId];
            END
        ");

        // 6. 為 ChartGroupId 欄位建立索引
        db.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes 
                WHERE name = N'IX_ControlChartTypes_ChartGroupId' AND object_id = OBJECT_ID(N'[ControlChartTypes]')
            )
            BEGIN
                CREATE INDEX [IX_ControlChartTypes_ChartGroupId] ON [ControlChartTypes] ([ChartGroupId]);
            END
        ");

        // 7. 建立 ControlChartTypes 對 ControlChartGroups 的新外鍵關聯
        db.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys 
                WHERE name = N'FK_ControlChartTypes_ControlChartGroups_ChartGroupId'
            )
            BEGIN
                ALTER TABLE [ControlChartTypes] ADD CONSTRAINT [FK_ControlChartTypes_ControlChartGroups_ChartGroupId] 
                FOREIGN KEY ([ChartGroupId]) REFERENCES [ControlChartGroups] ([Id]) ON DELETE NO ACTION;
            END
        ");

        // 8. 移除已無用處的 ControlChartCategories 表
        db.Database.ExecuteSqlRaw(@"
            IF OBJECT_ID(N'[ControlChartCategories]') IS NOT NULL
            BEGIN
                DROP TABLE [ControlChartCategories];
            END
        ");

        // 9. 將該 Migration ID 寫入歷史紀錄，避免 EF 再次嘗試執行該 Migration 丟出錯誤
        db.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260709080000_MergeGroupsAndCategories')
            BEGIN
                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                VALUES ('20260709080000_MergeGroupsAndCategories', '10.0.7');
            END
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during self-healing database migration: {ex.Message}");
    }
}
