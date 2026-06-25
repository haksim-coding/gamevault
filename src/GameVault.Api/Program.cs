using System.Text;
using GameVault.Api.Data;
using GameVault.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GameVault API",
        Version = "v1",
        Description = "Online users, profiles, public statistics and friends for GameVault."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter: Bearer {your JWT token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var configuredConnectionString = builder.Configuration.GetConnectionString("ServerDatabase")
    ?? throw new InvalidOperationException("Connection string 'ServerDatabase' is missing.");
var sqliteConnection = new SqliteConnectionStringBuilder(configuredConnectionString);
if (!Path.IsPathRooted(sqliteConnection.DataSource))
{
    sqliteConnection.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqliteConnection.DataSource);
}

builder.Services.AddDbContext<ServerDbContext>(options =>
    options.UseSqlite(sqliteConnection.ConnectionString));

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("JWT key is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();

public partial class Program;
