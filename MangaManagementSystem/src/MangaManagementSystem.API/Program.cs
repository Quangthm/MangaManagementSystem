using System.Text;
using MangaManagementSystem.Application;
using MangaManagementSystem.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MangaManagementSystem.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Application use-case services and Infrastructure (EF Core,
            // stored procedure wrappers, Cloudinary, OTP cache) are reused
            // as-is. The API only owns the HTTP boundary; it does not contain
            // business logic or SQL details.
            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructure(builder.Configuration);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is missing.");

            var jwtIssuer = builder.Configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("Jwt:Issuer is missing.");

            var jwtAudience = builder.Configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("Jwt:Audience is missing.");

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
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}