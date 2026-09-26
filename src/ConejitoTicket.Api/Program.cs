using System.Text;
using System.Text.Json.Serialization;
using ConejitoTicket.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
          ?? throw new InvalidOperationException("Falta la sección 'Jwt' en la configuración.");
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 bytes.");
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<TokenIssuer>();
builder.Services.AddScoped<RefreshTokens>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = jwt.SigningKey,
            NameClaimType = "name",
            RoleClaimType = "role",
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Roles.Admin, p => p.RequireRole(Roles.Admin))
    .AddPolicy(Roles.System, p => p.RequireRole(Roles.System));

builder.Services.Configure<VapidOptions>(builder.Configuration.GetSection("Vapid"));
builder.Services.AddSingleton<PushNotifier>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<PushNotifier>());

builder.Services.AddFeatures();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapFeatures();

await DbSeeder.SeedAsync(app);

app.Run();
