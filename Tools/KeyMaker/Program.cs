var builder = WebApplication.CreateBuilder(args);

var listenPort = Environment.GetEnvironmentVariable("APIPORT");
if (string.IsNullOrEmpty(listenPort)) listenPort = "31090";

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.ListenAnyIP(Convert.ToInt32(listenPort));
});

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

Console.WriteLine("KeyMaker listening on port " + listenPort);

app.Run();
