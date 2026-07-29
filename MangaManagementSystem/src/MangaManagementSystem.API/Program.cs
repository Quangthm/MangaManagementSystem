using System.Security.Claims;
using System.Text;
using MangaManagementSystem.Application;
using MangaManagementSystem.Application.Features.Ranking.Warnings;
using MangaManagementSystem.API.Endpoints;
using MangaManagementSystem.API.HostedServices;
using MangaManagementSystem.API.Security;
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

            var rankingWarningOptions =
                builder.Configuration
                    .GetSection(RankingWarningOptions.SectionName)
                    .Get<RankingWarningOptions>()
                ?? new RankingWarningOptions();

            builder.Services.AddSingleton(rankingWarningOptions);

            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddSingleton<DevelopmentTimeProvider>();
                builder.Services.AddSingleton<TimeProvider>(serviceProvider =>
                    serviceProvider.GetRequiredService<DevelopmentTimeProvider>());
            }
            else
            {
                builder.Services.AddSingleton(TimeProvider.System);
            }

            // Application use-case services and Infrastructure (EF Core,
            // stored procedure wrappers, Cloudinary, OTP cache) are reused
            // as-is. The API only owns the HTTP boundary; it does not contain
            // business logic or SQL details.
            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructure(builder.Configuration);

            builder.Services.AddHostedService<RankingWarningEvaluationHostedService>();

            builder.Services.AddControllers();
            builder.Services.AddScoped<
                IAuthenticatedActorResolver,
                AuthenticatedActorResolver>();
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

                    options.Events =
                        new JwtBearerEvents
                        {
                            OnTokenValidated =
                                async context =>
                                {
                                    var principal =
                                        context.Principal;

                                    if (principal is null)
                                    {
                                        context.Fail(
                                            "The authenticated identity is missing.");

                                        return;
                                    }

                                    var actorResolver =
                                        context.HttpContext
                                            .RequestServices
                                            .GetRequiredService<
                                                IAuthenticatedActorResolver>();

                                    var actor =
                                        await actorResolver
                                            .ResolveActiveUserAsync(
                                                principal);

                                    if (!actor.Succeeded)
                                    {
                                        context.Fail(
                                            "The authenticated account is no longer active.");

                                        return;
                                    }

                                    if (principal.Identity
                                        is ClaimsIdentity identity)
                                    {
                                        var existingRoleClaims =
                                            identity
                                                .FindAll(
                                                    ClaimTypes.Role)
                                                .ToArray();

                                        foreach (
                                            var roleClaim
                                            in existingRoleClaims)
                                        {
                                            identity.RemoveClaim(
                                                roleClaim);
                                        }

                                        if (!string.IsNullOrWhiteSpace(
                                                actor.ActorRoleName))
                                        {
                                            identity.AddClaim(
                                                new Claim(
                                                    ClaimTypes.Role,
                                                    actor.ActorRoleName));
                                        }
                                    }
                                }
                        };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.MapDevelopmentRankingWarningEndpoints();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}