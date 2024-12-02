using BackgroundProcessing;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<BackgroundProcessor>();
builder.Services.AddHttpClient<SomeService>();
builder.Services.AddOptions<BackgroundProcessingCfg>()
    .BindConfiguration(BackgroundProcessingCfg.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

app.MapGet("/async/{text}", (string text, SomeService someService, CancellationToken cancellationToken) =>
{
    for (var i = 0; i < 50; i++) 
        someService.DoAsyncBackground($"{text} - {i}", cancellationToken);
    
    return Results.Ok();
});

app.MapGet("/sync/{text}", (string text, SomeService someService, CancellationToken cancellationToken) =>
{
    for (var i = 0; i < 100; i++) 
        someService.DoSyncBackground($"{text} - {i}", cancellationToken);
    return Results.Ok();
});

app.Run();