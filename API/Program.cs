using API;
using RabbitMQUtils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<RabbitMqConnectionManager>(provider =>
    new RabbitMqConnectionManager("localhost", 5672, "guest", "guest"));
builder.Services.AddSingleton<RabbitMqSettingsManager>();
builder.Services.AddHostedService<RabbitMQConsumers>();
builder.Services.AddControllers();

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
