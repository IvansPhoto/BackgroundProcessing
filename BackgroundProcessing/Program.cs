using BackgroundProcessing;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<BackgroundProcessor>();
builder.Services.AddSingleton<BackgroundProcessorQueue>();
builder.Services.AddHttpClient<SomeService>(client => client.BaseAddress = new Uri(builder.Configuration.GetConnectionString("ServerUrl")!));

builder.Services.AddOptions<BackgroundProcessingCfg>()
    .BindConfiguration(BackgroundProcessingCfg.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

app.MapGet("/async/{text}/{jobNumber:int}",
    (string text, int jobNumber, SomeService someService, CancellationToken cancellationToken) =>
    {
        for (var i = 0; i < jobNumber; i++)
            someService.DoAsyncBackground($"{text} - {i}", cancellationToken);

        return Results.Ok();
    });

app.MapGet("/async-queue/{text}/{jobNumber:int}",
    async (string text, int jobNumber, SomeService someService, CancellationToken cancellationToken) =>
    {
        for (var i = 0; i < jobNumber; i++)
            await someService.DoAsyncBackgroundAwait($"{text} - {i}", cancellationToken);

        return Results.Ok();
    });

app.MapGet("/sync/{text}/{jobNumber:int}",
    (string text, int jobNumber, SomeService someService, CancellationToken cancellationToken) =>
    {
        for (var i = 0; i < jobNumber; i++)
            someService.DoSyncBackground($"{text} - {i}", cancellationToken);

        return Results.Ok();
    });

app.MapGet("/sync-queue/{text}/{jobNumber:int}",
    async (string text, int jobNumber, SomeService someService, CancellationToken cancellationToken) =>
    {
        for (var i = 0; i < jobNumber; i++)
            await someService.DoSyncBackgroundAwait($"{text} - {i}", cancellationToken);

        return Results.Ok();
    });

app.Run();