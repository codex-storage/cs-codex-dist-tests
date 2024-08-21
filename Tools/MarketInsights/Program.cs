using ArgsUniform;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace MarketInsights
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);
            var config = uniformArgs.Parse(true);

            var appState = new AppState(config);
            var updater = new Updater(appState);

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton(appState);

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(s =>
            {
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                s.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });

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

            updater.Run();
            app.Run();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("WebAPI for generating market overview for Codex network. Comes with OpenAPI swagger endpoint.");
        }
    }
}
