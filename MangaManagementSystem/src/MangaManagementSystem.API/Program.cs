using MangaManagementSystem.API.Options;
using MangaManagementSystem.Application;
using MangaManagementSystem.Infrastructure;

namespace MangaManagementSystem.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder =
                WebApplication.CreateBuilder(args);

            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructure(
                builder.Configuration);

            builder.Services
                .AddOptions<InternalApiOptions>()
                .Bind(
                    builder.Configuration.GetSection(
                        InternalApiOptions.SectionName))
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(
                            options.Key),
                    "InternalApi:Key is required.")
                .ValidateOnStart();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app =
                builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
