using ArgsUniform;
using System.Reflection;

namespace MarketInsights
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);
            var config = uniformArgs.Parse(true);
            var cts = new CancellationTokenSource();
            var appState = new AppState(config);

            Console.CancelKeyPress += (s, e) =>
            {
                appState.Log.Log("Stopping...");
                cts.Cancel();
                e.Cancel = true;
            };

            var connector = GethConnector.GethConnector.Initialize(appState.Log);
            if (connector == null) throw new Exception("Invalid Geth information");

            var updater = new Updater(appState, connector.CodexContracts, cts.Token);

            var builder = WebApplication.CreateBuilder(args);

            var listenPort = Environment.GetEnvironmentVariable("APIPORT");
            if (string.IsNullOrEmpty(listenPort)) listenPort = "31090";

            builder.WebHost.ConfigureKestrel((context, options) =>
            {
                options.ListenAnyIP(Convert.ToInt32(listenPort));
            });

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

            Console.WriteLine("MarketInsights listening on port " + listenPort);

            updater.Run();
            app.Run();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("WebAPI for generating market overview for Codex network. Comes with OpenAPI swagger endpoint.");

            var nl = Environment.NewLine;
            Console.WriteLine($"Required environment variables: {nl}" +
                $"'GETH_HOST'{nl}",
                $"'GETH_HTTP_PORT'{nl}",
                $"'CODEXCONTRACTS_MARKETPLACEADDRESS'{nl}",
                $"'CODEXCONTRACTS_TOKENADDRESS'{nl}",
                $"'CODEXCONTRACTS_ABI'{nl}");
        }
    }
}
