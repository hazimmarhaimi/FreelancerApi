using FreelancerAPI;
using FreelancerAPI.Data;
using FreelancerAPI.Dtos;
using FreelancerAPI.Enitites;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace FreelancerAPI.Tests;

public class FreelancersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public FreelancersControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("IntegrationTestDb"));

                // Ensure IMemoryCache is registered
                services.AddMemoryCache();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetFreelancer_ReturnsUnauthorized_WithoutToken()
    {
        // Act
        var response = await _client.GetAsync("/api/freelancers/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFreelancer_ReturnsOk_WithValidToken()
    {
        // Arrange: Seed the in-memory database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var freelancer = new Freelancer
        {
            Id = 1,
            Username = "john_doe",
            Email = "john.doe@example.com",
            PhoneNumber = "123-456-7890",
            IsArchived = false,
            Skillsets = new List<Skillset> { new Skillset { Name = "C#" } },
            Hobbies = new List<Hobby> { new Hobby { Name = "Reading" } }
        };
        dbContext.Freelancers.Add(freelancer);
        await dbContext.SaveChangesAsync();

        // Arrange: Get a token (valid until 05:31 PM +08 on May 20, 2025)
        var tokenResponse = await _client.GetAsync("/api/freelancers/generate-token");
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<Dictionary<string, string>>(tokenContent)["token"];
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/freelancers/1");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var freelancerDto = JsonSerializer.Deserialize<FreelancerDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("john_doe", freelancerDto.Username);
    }

    [Fact]
    public async Task CreateFreelancer_ReturnsCreated_WithValidData()
    {
        // Arrange: Get a token
        var tokenResponse = await _client.GetAsync("/api/freelancers/generate-token");
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<Dictionary<string, string>>(tokenContent)["token"];
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new
        {
            username = "jane_doe",
            email = "jane.doe@example.com",
            phoneNumber = "987-654-3210",
            skillsets = new[] { "JavaScript" },
            hobbies = new[] { "Hiking" }
        };
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(dto),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/freelancers/register", jsonContent);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdContent = await response.Content.ReadAsStringAsync();
        var createdResult = JsonSerializer.Deserialize<FreelancerDto>(createdContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("jane_doe", createdResult.Username);
    }

    [Fact]
    public async Task SearchFreelancers_ReturnsOk_WithValidQuery()
    {
        // Arrange: Seed the in-memory database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var freelancer = new Freelancer
        {
            Id = 1,
            Username = "john_doe",
            Email = "john.doe@example.com",
            PhoneNumber = "123-456-7890",
            IsArchived = false,
            Skillsets = new List<Skillset> { new Skillset { Name = "C#" } },
            Hobbies = new List<Hobby> { new Hobby { Name = "Reading" } }
        };
        dbContext.Freelancers.Add(freelancer);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/freelancers/search?query=john");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var freelancers = JsonSerializer.Deserialize<List<FreelancerDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Single(freelancers);
        Assert.Equal("john_doe", freelancers[0].Username);
    }

}