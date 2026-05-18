using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace MangaManagementSystem.Web.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            await builder.Build().RunAsync();
        }
    }
}
