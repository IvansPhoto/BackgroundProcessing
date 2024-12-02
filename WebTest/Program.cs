var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async () =>
{
    await Task.Delay(TimeSpan.FromSeconds(2));
    return Results.Ok();
});

app.Run();