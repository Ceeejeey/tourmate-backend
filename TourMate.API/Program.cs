using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TourMate.API.Data;
using TourMate.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        // Custom event handlers for clear unauthorized responses
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                // Skip the default behavior
                context.HandleResponse();

                // Set the response status code and content type
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                // Determine the error message
                var errorMessage = "Unauthorized: Authentication token is missing or invalid";
                if (!string.IsNullOrEmpty(context.ErrorDescription))
                {
                    errorMessage = context.ErrorDescription;
                }
                else if (!string.IsNullOrEmpty(context.Error))
                {
                    errorMessage = $"Unauthorized: {context.Error}";
                }

                // Return JSON response
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    statusCode = 401,
                    message = errorMessage,
                    error = "Unauthorized",
                    timestamp = DateTime.UtcNow
                });

                return context.Response.WriteAsync(result);
            },
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options => { options.AddPolicy("AllowFrontend", policy => { policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:3001").AllowAnyHeader().AllowAnyMethod(); }); });
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed database
await DbSeeder.SeedAdminAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
