using GateKeeper.Server.Interface;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Services;
using GateKeeper.Server.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using Hangfire;
using Hangfire.MySql;


var builder = WebApplication.CreateBuilder(args);

// Add user secrets in Development environment
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSingleton<IDBHelper, DBHelper>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IVerifyTokenService, VerifyTokenService>();
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
builder.Services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

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
    var config = sp.GetRequiredService<IConfiguration>();
    var dbHelper = sp.GetRequiredService<IDBHelper>();
    var logger = sp.GetRequiredService<ILogger<KeyManagementService>>();

    // Load the base64-encoded key from user secrets: "Encryption:MasterKey"
    var base64Key = config["Encryption:MasterKey"];
    if (string.IsNullOrEmpty(base64Key))
    {
        throw new InvalidOperationException("Encryption:MasterKey not found in user secrets.");
    }

    // Convert base64 => raw bytes (32 bytes = AES-256)
    var masterKeyBytes = Convert.FromBase64String(base64Key);

    return new KeyManagementService(dbHelper, logger, masterKeyBytes);
});


// Add JWT authentication to middleware
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
            ValidAudience = builder.Configuration["JwtConfig:Audience"],

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

// Schedule recurring job: RotateKeyAsync once every 24 hours
RecurringJob.AddOrUpdate<IKeyManagementService>(
    "rotate-keys-every-24hrs",
    service => service.RotateKeyAsync(DateTime.UtcNow.AddHours(24)),
    Cron.Daily // Runs every day at 00:00
);

app.Run();
