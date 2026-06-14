using MangaManagementSystem.Application;
using MangaManagementSystem.Infrastructure;

namespace MangaManagementSystem.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Application use-case services and Infrastructure (EF Core, stored procedure
            // wrappers, Cloudinary, OTP cache) are reused as-is. The API only owns the
            // HTTP boundary; it does not contain business logic or SQL details.
            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructure(builder.Configuration);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

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
