using CPMS.Services;
using Serilog.Events;
using Serilog;
using Serilog.Sinks.Kafka;
using CPMS.Models;
using CPMS.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Writers;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Kafka(
        bootstrapServers: "localhost:9092",
        topic: "premission-role-logs"
    )
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<MongoDBService>(serviceProvider =>
{
    var connectionString = builder.Configuration.GetConnectionString("MongoDB");

    if (connectionString == null || connectionString == string.Empty)
    {
        Log.Fatal("MongoDB ConnectionString can not be null!");
        Environment.Exit(1);
    }

    return new MongoDBService(connectionString, "permission_role_db");
});

builder.Services.AddTransient<SeedData>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");

var secretKey = jwtSettings["SecretKey"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];
//var expirationMintues = int.Parse(jwtSettings["AccessTokenExpirationMinutes"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = issuer,  // 权限服务签发者
        ValidAudience = audience,  // 受众（业务服务）
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)) // 解析密钥
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("Authentication failed: " + context.Exception.Message);
            Log.Information("Authentication failed: " + context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddTransient<JwtService>();

builder.Services.AddTransient<InitializeConstant>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// 初始化表單數據
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;

    try
    {
        // 獲取 MongoDB 服務
        var MongoDBService = serviceProvider.GetRequiredService<MongoDBService>();

        // 獲取 MongoDB Collection
        var seedData = serviceProvider.GetRequiredService<SeedData>();

        // 數據初始化
        seedData.Initialize();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex.Message);
    }
}

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;

    var initializeCount = serviceProvider.GetRequiredService<InitializeConstant>();

    initializeCount.Initialize();
}

app.Run();
