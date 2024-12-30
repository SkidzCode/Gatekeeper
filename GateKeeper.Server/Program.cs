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


builder.Services.AddScoped<IDBHelper, DBHelper>();
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
                QueuePollInterval = TimeSpan.FromSeconds(15), // Optional: Polling interval
                TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted, // MariaDB supports this isolation level
                JobExpirationCheckInterval = TimeSpan.FromMinutes(5) // Optional: How often expired jobs are checked
            }));
});

// Use this to configure both Hangfire and the server together
builder.Services.AddHangfireServer(options =>
{
    options.ServerTimeout = TimeSpan.FromMinutes(1); // Automatically removes inactive servers after 1 minute
    options.ServerName = $"HangfireServer-{Environment.MachineName}"; // Unique server name
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtConfig:Secret"]))
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

app.Run();
