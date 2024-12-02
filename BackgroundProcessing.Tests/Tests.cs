using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace BackgroundProcessing.Tests;

public partial class Tests
{
    private readonly WireMockServer _server = WireMockServer.Start(settings =>
    {
        settings.Urls = ["http://*:20001/"];
        settings.AcceptAnyClientCertificate = true;
    });

    private static readonly HttpClient HttpClient = new();
    private IFutureDockerImage? _image;
    private IContainer? _container;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
            .WithDockerfile("BackgroundProcessing/Dockerfile")
            .WithLabel("name", "background-processing")
            .WithName("background-processing")
            .WithDeleteIfExists(true)
            .WithCleanUp(true)
            .Build();
        await _image.CreateAsync();

        _container = new ContainerBuilder()
            .WithImage(_image!.FullName)
            .WithPortBinding(8080, true)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(MyRegex()))
            .WithCleanUp(true)
            .Build();
        await _container.StartAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _server.Reset();
    }
    
    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_image is not null)
            await _image.DisposeAsync();
        if (_container is not null)
            await _container.DisposeAsync();

        _server.Dispose();
    }
    
    [Test]
    public async Task Test1()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/get").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(2)));

        // Act
        var result = await HttpClient.GetAsync(new Uri($"http://{_container!.Hostname}:{_container.GetMappedPublicPort(8080)}/async/text/100"));
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Arrange
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccessStatusCode, Is.True);
            Assert.That(_server.LogEntries.Count(), Is.LessThan(25));
        });
    }
    
    [Test]
    public async Task Test2()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/get").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(2)));

        // Act
        var result = await HttpClient.GetAsync(new Uri($"http://{_container!.Hostname}:{_container.GetMappedPublicPort(8080)}/async/text/100"));
        await Task.Delay(TimeSpan.FromSeconds(60));

        // Arrange
        Assert.That(_server.LogEntries.Count(), Is.EqualTo(50));
    }

    [GeneratedRegex(".*Now listening on.*")]
    private static partial Regex MyRegex();
}