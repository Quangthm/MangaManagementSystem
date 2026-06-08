using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Application.Services;
using MangaManagementSystem.Infrastructure;

namespace MangaManagementSystem.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Register Infrastructure services:
            // DbContext, repositories, UnitOfWork, file storage, email service...
            builder.Services.AddInfrastructure(builder.Configuration);

            // Register Application services.
            builder.Services.AddScoped<ISeriesService, SeriesService>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
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