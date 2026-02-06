using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OneClick.Server.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. DB Connection (PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. JWT Authentication Setup
var jwtKey = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ?? "");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register HttpClient for external API calls (e.g. Toss Payments)
// Register HttpClient for external API calls (e.g. Toss Payments)
builder.Services.AddHttpClient();

// Register Automation Service & Modules
// Register HttpClient for external API calls (e.g. Toss Payments)
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable serving updates (allow RELEASES and .nupkg)
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true, // Allow files without extension (RELEASES)
    DefaultContentType = "application/octet-stream"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllers();

// [중요] DB 초기화 및 데이터 시딩
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OneClick.Server.Data.AppDbContext>();
    
    // 1. 기존 DB 삭제 (스키마 변경 반영을 위해) - 개발용
    // 주의: 모든 데이터가 삭제됩니다. 이제 스키마가 잡혔으니 주석 처리합니다.
    // context.Database.EnsureDeleted();
    
    // 2. 새 스키마로 DB 생성 (DB가 없으면 생성, 있으면 무시)
    // context.Database.EnsureCreated();
    
    // 3. 초기 데이터 주입 (SNS 카테고리, 카카오톡 봇 등)
    // await OneClick.Server.Data.DataSeeder.SeedAsync(context);
}

app.Run();
