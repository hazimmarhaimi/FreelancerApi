using FreelancerAPI.Controllers;
using FreelancerAPI.Data;
using FreelancerAPI.Dtos;
using FreelancerAPI.Enitites;
using FreelancerAPI.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace FreelancerAPI.Tests;

public class FreelancersControllerTests
{
    private readonly Mock<IRepository<Freelancer>> _mockFreelancerRepository;
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly FreelancersController _controller;
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;

    public FreelancersControllerTests()
    {
        _mockFreelancerRepository = new Mock<IRepository<Freelancer>>();
        _mockContext = new Mock<ApplicationDbContext>(new DbContextOptionsBuilder<ApplicationDbContext>().Options);
        _mockConfiguration = new Mock<IConfiguration>();
        _mockMemoryCache = new Mock<IMemoryCache>();

        _controller = new FreelancersController(_mockFreelancerRepository.Object, _mockContext.Object, _mockConfiguration.Object, _mockMemoryCache.Object);

        _cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task GetFreelancer_ReturnsOk_WhenFreelancerExists()
    {
        // Arrange
        var freelancerId = 1;
        var freelancer = new Freelancer
        {
            Id = freelancerId,
            Username = "john_doe",
            Email = "john.doe@example.com",
            PhoneNumber = "123-456-7890",
            IsArchived = false,
            Skillsets = new List<Skillset> { new Skillset { Name = "C#" } },
            Hobbies = new List<Hobby> { new Hobby { Name = "Reading" } }
        };

        _mockFreelancerRepository.Setup(repo => repo.GetByIdAsync(freelancerId))
            .ReturnsAsync(freelancer);

        // Mock cache to return false (not found in cache)
        object cachedValue;
        _mockMemoryCache.Setup(m => m.TryGetValue($"Freelancer_{freelancerId}", out cachedValue))
            .Returns(false)
            .Callback(() => cachedValue = null); // Ensure cachedValue is set to null when not found

        // Mock cache Set operation to ensure it succeeds
        _mockMemoryCache.Setup(m => m.Set($"Freelancer_{freelancerId}", It.IsAny<FreelancerDto>(), _cacheEntryOptions))
            .Callback<string, FreelancerDto, MemoryCacheEntryOptions>((key, value, options) => { });

        // Set up ControllerContext with more context
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider()
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.GetFreelancer(freelancerId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<FreelancerDto>>(result);
        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result); // Use base ObjectResult
        Assert.Equal(StatusCodes.Status200OK, objectResult.StatusCode); // Verify status code
        var freelancerDto = Assert.IsType<FreelancerDto>(objectResult.Value);
        Assert.Equal(freelancerId, freelancerDto.Id);
        Assert.Equal("john_doe", freelancerDto.Username);
        _mockMemoryCache.Verify(m => m.Set($"Freelancer_{freelancerId}", It.IsAny<FreelancerDto>(), _cacheEntryOptions), Times.Once());
    }
    [Fact]
    public async Task GetFreelancer_ReturnsNotFound_WhenFreelancerDoesNotExist()
    {
        // Arrange
        var freelancerId = 1;
        _mockFreelancerRepository.Setup(repo => repo.GetByIdAsync(freelancerId))
            .ReturnsAsync((Freelancer)null);

        // Mock cache to return false
        object cachedValue;
        _mockMemoryCache.Setup(m => m.TryGetValue($"Freelancer_{freelancerId}", out cachedValue))
            .Returns(false)
            .Callback(() => cachedValue = null);

        // Set up ControllerContext to simulate HTTP context
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.GetFreelancer(freelancerId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var json = JsonSerializer.Serialize(notFoundResult.Value); // Serialize the anonymous object
        var response = JsonSerializer.Deserialize<ResponseDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); // Deserialize to ResponseDto
        Assert.NotNull(response);
        Assert.Equal(StatusCodes.Status404NotFound, response.Status);
        Assert.Equal($"Freelancer with ID {freelancerId} not found.", response.Message);
    }

    [Fact]
    public async Task CreateFreelancer_ReturnsCreated_WhenInputIsValid()
    {
        // Arrange
        var dto = new CreateFreelancerDto(
            "jane_doe",
            "jane.doe@example.com",
            "987-654-3210",
            new List<string> { "JavaScript" },
            new List<string> { "Hiking" }
        );

        var freelancer = new Freelancer
        {
            Id = 1,
            Username = dto.Username,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Skillsets = dto.Skillsets.Select(s => new Skillset { Name = s }).ToList(),
            Hobbies = dto.Hobbies.Select(h => new Hobby { Name = h }).ToList()
        };

        // Mock repository to assign ID and complete the operation
        _mockFreelancerRepository.Setup(repo => repo.AddAsync(It.IsAny<Freelancer>()))
            .Callback<Freelancer>(f => f.Id = 1)
            .Returns(Task.CompletedTask);

        // Mock cache to verify removal
        var cacheRemoved = false;
        _mockMemoryCache.Setup(m => m.Remove($"Freelancer_1"))
            .Callback(() => cacheRemoved = true);

        // Act
        var result = await _controller.CreateFreelancer(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var freelancerDto = Assert.IsType<FreelancerDto>(createdResult.Value);
        Assert.Equal("jane_doe", freelancerDto.Username);
        Assert.Equal("GetFreelancer", createdResult.ActionName);
        Assert.Equal(1, createdResult.RouteValues["id"]);
        Assert.True(cacheRemoved, "Cache should be invalidated for Freelancer_1");
        _mockMemoryCache.Verify(m => m.Remove($"Freelancer_1"), Times.Once());
    }

    [Fact]
    public async Task SearchFreelancers_ReturnsOk_WhenQueryIsValid()
    {
        // Arrange
        var query = "john";
        var page = 1;
        var pageSize = 10;
        var freelancers = new List<Freelancer>
    {
        new Freelancer
        {
            Id = 1,
            Username = "john_doe",
            Email = "john.doe@example.com",
            PhoneNumber = "123-456-7890",
            IsArchived = false,
            Skillsets = new List<Skillset> { new Skillset { Name = "C#" } },
            Hobbies = new List<Hobby> { new Hobby { Name = "Reading" } }
        }
    };

        var mockSet = new Mock<DbSet<Freelancer>>();
        mockSet.As<IQueryable<Freelancer>>().Setup(m => m.Provider).Returns(freelancers.AsQueryable().Provider);
        mockSet.As<IQueryable<Freelancer>>().Setup(m => m.Expression).Returns(freelancers.AsQueryable().Expression);
        mockSet.As<IQueryable<Freelancer>>().Setup(m => m.ElementType).Returns(freelancers.AsQueryable().ElementType);
        mockSet.As<IQueryable<Freelancer>>().Setup(m => m.GetEnumerator()).Returns(freelancers.AsQueryable().GetEnumerator());

        _mockContext.Setup(c => c.Freelancers).Returns(mockSet.Object);
        _mockContext.Setup(c => c.Freelancers.Include(It.IsAny<string>())).Returns(mockSet.Object);

        // Mock cache to return false
        object cachedValue;
        _mockMemoryCache.Setup(m => m.TryGetValue($"SearchFreelancers_{query}_{page}_{pageSize}", out cachedValue))
            .Returns(false);

        // Act
        var result = await _controller.SearchFreelancers(query, page, pageSize);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var freelancerDtos = Assert.IsType<List<FreelancerDto>>(okResult.Value);
        Assert.Single(freelancerDtos);
        Assert.Equal("john_doe", freelancerDtos[0].Username);
        _mockMemoryCache.Verify(m => m.Set($"SearchFreelancers_{query}_{page}_{pageSize}", It.IsAny<IEnumerable<FreelancerDto>>(), _cacheEntryOptions), Times.Once());
    }

}