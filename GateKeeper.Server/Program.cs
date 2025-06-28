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
using System.Data; // Added for IDbConnection
using MySqlConnector; // Added for MySqlConnection
using GateKeeper.Plugin.Abstractions;

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

// Register IDbConnection for Dapper repositories
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var dbConfig = sp.GetRequiredService<IOptions<DatabaseConfig>>().Value;
    return new MySqlConnection(dbConfig.GateKeeperConnection);
});

// Register Repositories
builder.Services.AddScoped<IUserRepository, GateKeeper.Server.Repositories.UserRepository>();
builder.Services.AddScoped<IRoleRepository, GateKeeper.Server.Repositories.RoleRepository>();
builder.Services.AddScoped<ISettingsRepository, GateKeeper.Server.Repositories.SettingsRepository>();
builder.Services.AddScoped<IVerifyTokenRepository, GateKeeper.Server.Repositories.VerifyTokenRepository>();
builder.Services.AddScoped<IUserAuthenticationRepository, GateKeeper.Server.Repositories.UserAuthenticationRepository>();
builder.Services.AddScoped<IInviteRepository, GateKeeper.Server.Repositories.InviteRepository>();
builder.Services.AddScoped<IKeyManagementRepository, GateKeeper.Server.Repositories.KeyManagementRepository>();
builder.Services.AddScoped<INotificationRepository, GateKeeper.Server.Repositories.NotificationRepository>();
builder.Services.AddScoped<INotificationTemplateRepository, GateKeeper.Server.Repositories.NotificationTemplateRepository>();
builder.Services.AddScoped<ISessionRepository, GateKeeper.Server.Repositories.SessionRepository>();

// Register Services
builder.Services.AddScoped<IEmailService, EmailService>(); // EmailService doesn't use a repository
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IResourceService, ResourceService>(); // ResourceService interacts with file system
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

builder.Services.AddScoped<IKeyManagementService>(sp => // Changed from AddSingleton to AddScoped
{
    var keyManagementRepository = sp.GetRequiredService<IKeyManagementRepository>();
    var logger = sp.GetRequiredService<ILogger<KeyManagementService>>();
    var keyManagementConfigOptions = sp.GetRequiredService<IOptions<KeyManagementConfig>>();

    // KeyManagementService is refactored to take IKeyManagementRepository, ILogger, and IOptions<KeyManagementConfig>.
    // The validation for MasterKey (presence, Base64 format, length) is handled within KeyManagementService.
    return new KeyManagementService(
        keyManagementRepository,
        logger,
        keyManagementConfigOptions
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

#region Plugin Discovery and Loading
Log.Information("Starting plugin discovery...");
var loadedPlugins = new List<IPlugin>();

// Define the path to the plugins directory
string pluginsPath = Path.Combine(AppContext.BaseDirectory, "plugins");
Log.Information("Looking for plugins in: {PluginsPath}", pluginsPath);

if (Directory.Exists(pluginsPath))
{
    // Load assemblies from the plugins directory
    foreach (string dllPath in Directory.GetFiles(pluginsPath, "*.dll"))
    {
        try
        {
            Log.Debug("Attempting to load assembly: {DllPath}", dllPath);
            Assembly assembly = Assembly.LoadFrom(dllPath);
            Log.Information("Successfully loaded assembly: {AssemblyName}", assembly.FullName);

            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in pluginTypes)
            {
                try
                {
                    var plugin = (IPlugin)Activator.CreateInstance(type);
                    if (plugin != null)
                    {
                        Log.Information("Found plugin: {PluginName} v{PluginVersion} from {AssemblyFullName}", plugin.Name, plugin.Version, assembly.FullName);
                        plugin.ConfigureServices(builder.Services, builder.Configuration);
                        loadedPlugins.Add(plugin);
                        Log.Information("Configured plugin: {PluginName}", plugin.Name);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error instantiating or configuring plugin type {PluginType} from assembly {AssemblyName}", type.FullName, assembly.FullName);
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
             Log.Warning("Could not load types from assembly {DllPath} during plugin discovery: {LoaderExceptions}", dllPath, string.Join(", ", ex.LoaderExceptions.Select(e => e?.Message ?? "N/A")));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error loading or inspecting assembly {DllPath} for plugins", dllPath);
        }
    }
}
else
{
    Log.Warning("Plugins directory not found: {PluginsPath}", pluginsPath);
}

// Also scan already loaded assemblies (e.g., if a plugin is directly referenced by the server project)
var alreadyLoadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
foreach (var assembly in alreadyLoadedAssemblies)
{
    // Avoid reprocessing assemblies already loaded from the 'plugins' directory
    if (Directory.Exists(pluginsPath) && assembly.IsDynamic == false && !string.IsNullOrEmpty(assembly.Location))
    {
        try
        {
            if (Path.GetDirectoryName(assembly.Location).Equals(pluginsPath, StringComparison.OrdinalIgnoreCase))
            {
                // Log.Debug("Skipping already processed assembly from plugins folder: {AssemblyName}", assembly.FullName);
                continue;
            }
        }
        catch (NotSupportedException)
        {
            // Assembly.Location can throw NotSupportedException for dynamic assemblies, skip them.
            // Log.Debug("Skipping dynamic assembly or assembly without location: {AssemblyName}", assembly.FullName);
            continue;
        }
    }

    try
    {
        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && !loadedPlugins.Any(lp => lp.GetType() == t));

        foreach (var type in pluginTypes)
        {
            try
            {
                // Check if a plugin with the same name and version has already been loaded (e.g. from the 'plugins' folder)
                var tempPlugin = (IPlugin)Activator.CreateInstance(type); // Create temporary instance for checking
                if (tempPlugin != null && !loadedPlugins.Any(lp => lp.Name == tempPlugin.Name && lp.Version == tempPlugin.Version))
                {
                    Log.Information("Found plugin from already loaded assembly: {PluginName} v{PluginVersion} from {AssemblyFullName}", tempPlugin.Name, tempPlugin.Version, assembly.FullName);
                    tempPlugin.ConfigureServices(builder.Services, builder.Configuration); // Configure the actual instance
                    loadedPlugins.Add(tempPlugin); // Add the actual instance
                    Log.Information("Configured plugin: {PluginName}", tempPlugin.Name);
                }
                else if (tempPlugin != null)
                {
                    Log.Debug("Plugin {PluginName} v{PluginVersion} from {AssemblyFullName} was already loaded (likely from plugins folder). Skipping.", tempPlugin.Name, tempPlugin.Version, assembly.FullName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error instantiating or configuring plugin type {PluginType} from already loaded assembly {AssemblyName}", type.FullName, assembly.FullName);
            }
        }
    }
    catch (ReflectionTypeLoadException ex)
    {
        Log.Warning("Could not load types from already loaded assembly {AssemblyName} during plugin discovery: {LoaderExceptions}", assembly.FullName, string.Join(", ", ex.LoaderExceptions.Select(e => e?.Message ?? "N/A")));
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Error inspecting already loaded assembly {AssemblyName} for plugins", assembly.FullName);
    }
}

if (!loadedPlugins.Any())
{
    Log.Information("No plugins found after checking filesystem and loaded assemblies.");
}
else
{
    Log.Information("Finished plugin discovery. Loaded {PluginCount} plugins in total.", loadedPlugins.Count);
}
builder.Services.AddSingleton<IReadOnlyList<IPlugin>>(loadedPlugins.AsReadOnly());
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
