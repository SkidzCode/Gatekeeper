using GateKeeper.Server.Interface;
using GateKeeper.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Hangfire;
using Hangfire.MySql;
using Serilog;
using GateKeeper.Server.Middleware;
using Serilog.Formatting.Compact;
using GateKeeper.Server.Models.Configuration; // Added for typed configurations
using Microsoft.Extensions.Options; // Added for IOptions

var builder = WebApplication.CreateBuilder(args);

// Add user secrets in Development environment
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

#region Add Serilog

// Read the EnableHashing setting
var serilogConfigOptions = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<SerilogConfig>>();
bool enableHashing = serilogConfigOptions.Value.EnableHashing;

// Configure Serilog
var loggerConfiguration = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Filter.ByExcluding(logEvent =>
    {
        // Example: exclude logs that may contain full user data
        return false; // Modify filter as needed
    });

if (enableHashing)
{
    // Use the custom ChainedFileSink
    loggerConfiguration = loggerConfiguration
        .WriteTo.ChainedFile(
            mainLogDirectory: "Logs",
            hashesOnlyDirectory: "Logs",
            fileNamePrefix: "chained-log-rotating"
        );
}
else
{
    // Use Serilog's default rolling file sink
    loggerConfiguration = loggerConfiguration
        .WriteTo.File(
            formatter: new CompactJsonFormatter(),
            path: "Logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: null // Adjust as needed
        );
}

Log.Logger = loggerConfiguration.CreateLogger();

// Use Serilog as the logging provider
builder.Host.UseSerilog(Log.Logger);

#endregion

#region Gatekeeper

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDataProtection();
builder.Services.AddHttpContextAccessor();

// Register and Validate Typed Configuration
builder.Services.AddOptions<EmailSettingsConfig>()
    .Bind(builder.Configuration.GetSection(EmailSettingsConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<DatabaseConfig>()
    .Bind(builder.Configuration.GetSection("ConnectionStrings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<JwtSettingsConfig>()
    .Bind(builder.Configuration.GetSection(JwtSettingsConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<KeyManagementConfig>()
    .Bind(builder.Configuration.GetSection(KeyManagementConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<PasswordSettingsConfig>() // Added PasswordSettingsConfig
    .Bind(builder.Configuration.GetSection(PasswordSettingsConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RegisterSettingsConfig>()
    .Bind(builder.Configuration.GetSection(RegisterSettingsConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<LoginSettingsConfig>()
    .Bind(builder.Configuration.GetSection(LoginSettingsConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ResourceSettingsConfig>()
    .Bind(builder.Configuration.GetSection(ResourceSettingsConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SerilogConfig>()
    .Bind(builder.Configuration.GetSection(SerilogConfig.SectionName));
// End Register and Validate Typed Configuration

builder.Services.AddSingleton<IDbHelper, DBHelper>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IVerifyTokenService, VerifyTokenService>();
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
builder.Services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IInviteService, InviteService>();
builder.Services.AddScoped<ISessionService, SessionService>();

// Register IStringDataProtector and its wrapper
builder.Services.AddTransient<IStringDataProtector>(provider =>
    new StringDataProtectorWrapper(
        provider.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>()
            .CreateProtector("SecureCookies")
    )
);

#endregion

#region Hangfire

builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseStorage(new MySqlStorage(
            builder.Configuration.GetConnectionString("HangfireConnection"),
            new MySqlStorageOptions
            {
                QueuePollInterval = TimeSpan.FromSeconds(15),
                TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
                JobExpirationCheckInterval = TimeSpan.FromMinutes(5)
            }));
});

// Configure Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.ServerTimeout = TimeSpan.FromMinutes(1);
    options.ServerName = $"HangfireServer-{Environment.MachineName}";
});

#endregion

#region Swagger
builder.Services.AddSwaggerGen(options =>
{
    // Add a security definition for JWT Bearer
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token.\r\n\r\nExample: `Bearer abc123def456`",
    });
    options.EnableAnnotations();

    // Apply the security rule (requirement) globally to your API
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>() // No specific scopes required
        }
    });

    // (Optional) Include XML comments if you want to show summaries for actions
    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlFilePath = Path.Combine(AppContext.BaseDirectory, xmlFileName);
    if (File.Exists(xmlFilePath))
    {
        options.IncludeXmlComments(xmlFilePath);
    }
});
#endregion

#region JWT Middleware

builder.Services.AddSingleton<IKeyManagementService>(sp =>
{
    var keyManagementConfig = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KeyManagementConfig>>().Value;
    var dbHelper = sp.GetRequiredService<IDbHelper>();
    var logger = sp.GetRequiredService<ILogger<KeyManagementService>>();

    // KeyManagementService is already refactored to take IOptions<KeyManagementConfig> in its constructor.
    // The validation for MasterKey (presence, Base64 format, length) is handled within KeyManagementService.
    return new KeyManagementService(
        dbHelper, 
        logger, 
        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KeyManagementConfig>>() // Pass IOptions directly
    );
});

// Add JWT authentication to middleware
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // JwtSettings will be injected by the options framework
        var jwtSettingsProvider = builder.Services.BuildServiceProvider().GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtSettingsConfig>>();
        var jwtSettings = jwtSettingsProvider.Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,

            // 1) The magic: a custom key resolver
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                // 2) Grab your service from DI. 
                // Because we are in Program.cs, you might do a trick like:
                var kms = builder.Services.BuildServiceProvider().GetRequiredService<IKeyManagementService>();

                // 3) Retrieve the current key (or multiple keys)
                var secureKey = kms.GetCurrentKeyAsync().Result;
                if (secureKey == null)
                    return Array.Empty<SecurityKey>();

                // 4) Convert from SecureString => bytes => SymmetricSecurityKey
                byte[] keyBytes;
                var bstrPtr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(secureKey);
                try
                {
                    var base64Key = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(bstrPtr);
                    keyBytes = Convert.FromBase64String(base64Key);
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(bstrPtr);
                }

                var dynamicKey = new SymmetricSecurityKey(keyBytes);

                // 5) Return an array of possible keys
                return new[] { dynamicKey };
            }
        };
    });

builder.Services.AddAuthorization();
#endregion

var app = builder.Build();

app.UseMiddleware<LogEnrichmentMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>(); // Added Global Exception Handler

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("/index.html");

#region Hangfire Scheduled Tasks

// Schedule recurring job: RotateKeyAsync once every 24 hours
RecurringJob.AddOrUpdate<IKeyManagementService>(
    "rotate-keys-every-24hrs",
    service => service.RotateKeyAsync(DateTime.UtcNow.AddHours(24)),
    Cron.Daily // Runs every day at 00:00
);

// Schedule recurring job: ProcessPendingNotificationsAsync every 5 minutes
RecurringJob.AddOrUpdate<INotificationService>(
    "process-pending-notifications",
    service => service.ProcessPendingNotificationsAsync(),
    Cron.MinuteInterval(5) // Runs every 5 minutes
);

#endregion



try
{
    Log.Information("Starting up the application..."); 
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
