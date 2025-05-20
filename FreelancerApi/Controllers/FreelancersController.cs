using FreelancerAPI.Data;
using FreelancerAPI.Dtos;
using FreelancerAPI.Enitites;
using FreelancerAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace FreelancerAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FreelancersController : ControllerBase
{
    private readonly IRepository<Freelancer> _freelancerRepository; 
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;

    public FreelancersController(IRepository<Freelancer> freelancerRepository, ApplicationDbContext context, IConfiguration configuration, IMemoryCache memoryCache)
    {
        _freelancerRepository = freelancerRepository ?? throw new ArgumentNullException(nameof(freelancerRepository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    #region Standard CRUD operations
    // Get all freelancers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FreelancerDto>>> GetFreelancers()
    {
        try
        {
            var freelancers = await _freelancerRepository.GetAllAsync();
            var freelancerDtos = freelancers.Select(f => new FreelancerDto(
                f.Id,
                f.Username,
                f.Email,
                f.PhoneNumber,
                f.IsArchived,
                f.Skillsets.Select(s => s.Name).ToList(),
                f.Hobbies.Select(h => h.Name).ToList()
            )).ToList();
            return Ok(freelancerDtos);
        } catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = StatusCodes.Status500InternalServerError,
                message = "An error occurred while retrieving freelancers.",
                details = ex.Message
            });
        }
    }

    // Get freelancer by id
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<FreelancerDto>> GetFreelancer(int id)
    {
        try
        {
            // Define a cache key based on the id
            string cacheKey = $"Freelancer_{id}";

            // Check if the data is in the cache
            if (_memoryCache.TryGetValue(cacheKey, out FreelancerDto cachedFreelancer))
            {
                return Ok(cachedFreelancer); // Return cached data
            }

            // Fetch from database if not in cache
            var freelancer = await _freelancerRepository.GetByIdAsync(id);
            if (freelancer == null)
                return NotFound(new
                {
                    status = StatusCodes.Status404NotFound,
                    message = $"Freelancer with ID {id} not found."
                });

            var freelancerDto = new FreelancerDto(
                freelancer.Id,
                freelancer.Username,
                freelancer.Email,
                freelancer.PhoneNumber,
                freelancer.IsArchived,
                freelancer.Skillsets.Select(s => s.Name).ToList(),
                freelancer.Hobbies.Select(h => h.Name).ToList()
            );

            // Cache the result with a 5-minute expiration
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            _memoryCache.Set(cacheKey, freelancerDto, cacheEntryOptions);

            return Ok(freelancerDto);
        } catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = StatusCodes.Status500InternalServerError,
                message = "An error occurred while retrieving the freelancer.",
                details = ex.Message
            });
        }
    }

    // Create freelancer
    [HttpPost("register")]
    public async Task<ActionResult<FreelancerDto>> CreateFreelancer(CreateFreelancerDto dto)
    {
        try
        {
            if (string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.Email))
            {
                return BadRequest(new
                {
                    status = StatusCodes.Status400BadRequest,
                    message = "Username and Email are required."
                });
            }

            var freelancer = new Freelancer
            {
                Username = dto.Username,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Skillsets = dto.Skillsets?.Select(s => new Skillset { Name = s }).ToList() ?? new List<Skillset>(),
                Hobbies = dto.Hobbies?.Select(h => new Hobby { Name = h }).ToList() ?? new List<Hobby>()
            };

            await _freelancerRepository.AddAsync(freelancer);

            var freelancerDto = new FreelancerDto(
                freelancer.Id,
                freelancer.Username,
                freelancer.Email,
                freelancer.PhoneNumber,
                freelancer.IsArchived,
                freelancer.Skillsets.Select(s => s.Name).ToList(),
                freelancer.Hobbies.Select(h => h.Name).ToList()
            );

            // Invalidate cache for this freelancer
            _memoryCache.Remove($"Freelancer_{freelancer.Id}");

            return CreatedAtAction(nameof(GetFreelancer), new { id = freelancer.Id }, freelancerDto);
        } catch (DbUpdateException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = StatusCodes.Status500InternalServerError,
                message = "Database error occurred while creating the freelancer.",
                details = ex.InnerException?.Message ?? ex.Message
            });
        } catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = StatusCodes.Status500InternalServerError,
                message = "An error occurred while creating the freelancer.",
                details = ex.Message
            });
        }
    }
    // Update freelancer data
    [HttpPut("update/{id}")]
    public async Task<IActionResult> UpdateFreelancer(int id, UpdateFreelancerDto dto)
    {
        try
        {
            if (string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.Email))
            {
                return BadRequest(new
                {
                    status = StatusCodes.Status400BadRequest,
                    message = "Username and Email are required."
                });
            }

            var freelancer = await _freelancerRepository.GetByIdAsync(id);
            if (freelancer == null)
                return NotFound(new
                {
                    status = StatusCodes.Status404NotFound,
                    message = $"Freelancer with ID {id} not found."
                });

            freelancer.Username = dto.Username;
            freelancer.Email = dto.Email;
            freelancer.PhoneNumber = dto.PhoneNumber;
            freelancer.Skillsets = dto.Skillsets?.Select(s => new Skillset { Name = s, FreelancerId = id }).ToList() ?? new List<Skillset>();
            freelancer.Hobbies = dto.Hobbies?.Select(h => new Hobby { Name = h, FreelancerId = id }).ToList() ?? new List<Hobby>();

            await _freelancerRepository.UpdateAsync(freelancer);

            // Invalidate cache for this freelancer
            _memoryCache.Remove($"Freelancer_{id}");

            return Ok(new
            {
                status = StatusCodes.Status200OK,
                message = $"Freelancer with ID {id} successfully updated."
            });
        } catch (DbUpdateException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = StatusCodes.Status500InternalServerError,
                message = "Database error occurred while updating the freelancer.",
                details = ex.InnerException?.Message ?? ex.Message
            });
        } catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = StatusCodes.Status500InternalServerError,
                message = "An error occurred while updating the freelancer.",
                details = ex.Message
            });
        }
    }
    // Delete freelancer
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteFreelancer(int id)
    {
        try
        {
            var freelancer = await _freelancerRepository.GetByIdAsync(id);
            if (freelancer == null)
                return NotFound(new
                {
                    status = StatusCodes.Status404NotFound,
                    message = $"Freelancer with ID {id} not found."
                });

            await _freelancerRepository.DeleteAsync(freelancer);

            // Invalidate cache for this freelancer
            _memoryCache.Remove($"Freelancer_{id}");

            return NoContent();
        } catch (DbUpdateException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = StatusCodes.Status500InternalServerError,
                message = "Database error occurred while deleting the freelancer.",
                details = ex.InnerException?.Message ?? ex.Message
            });
        } catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = StatusCodes.Status500InternalServerError,
                message = "An error occurred while deleting the freelancer.",
                details = ex.Message
            });
        }
    }
    #endregion

    #region Wildcard search
    // Wildcard search
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<FreelancerDto>>> SearchFreelancers([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (string.IsNullOrEmpty(query))
                return BadRequest(new
                {
                    status = StatusCodes.Status400BadRequest,
                    message = "Search query is required."
                });

            // Define a cache key based on query, page, and pageSize
            string cacheKey = $"SearchFreelancers_{query}_{page}_{pageSize}";

            // Check if the data is in the cache
            if (_memoryCache.TryGetValue(cacheKey, out IEnumerable<FreelancerDto> cachedFreelancers))
            {
                Response.Headers.Add("X-Total-Count", cachedFreelancers.Count().ToString());
                return Ok(cachedFreelancers); // Return cached data
            }

            var freelancers = await _context.Freelancers
                .Include(f => f.Skillsets)
                .Include(f => f.Hobbies)
                .Where(f => f.Username.Contains(query) || f.Email.Contains(query))
                .ToListAsync();

            if (freelancers.Count == 0)
                return Ok(new
                {
                    status = StatusCodes.Status200OK,
                    message = "No data found."
                });

            var totalCount = freelancers.Count();
            var pagedFreelancers = freelancers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FreelancerDto(
                    f.Id,
                    f.Username,
                    f.Email,
                    f.PhoneNumber,
                    f.IsArchived,
                    f.Skillsets.Select(s => s.Name).ToList(),
                    f.Hobbies.Select(h => h.Name).ToList()
                )).ToList();

            // Cache the result with a 5-minute expiration
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            _memoryCache.Set(cacheKey, pagedFreelancers, cacheEntryOptions);

            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            return Ok(pagedFreelancers);
        } catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = StatusCodes.Status500InternalServerError,
                message = "An error occurred while searching for freelancers.",
                details = ex.Message
            });
        }
    }
    #endregion

    #region Archive/Unarchive
    // Archive and unarchive freelancers
    [HttpPut("{id}/archive")]
    public async Task<IActionResult> ArchiveFreelancer(int id)
    {
        try
        {
            var freelancer = await _freelancerRepository.GetByIdAsync(id);
            if (freelancer == null)
                return NotFound(new
                {
                    status = StatusCodes.Status404NotFound,
                    message = $"Freelancer with ID {id} not found."
                });

            freelancer.IsArchived = true;
            await _freelancerRepository.UpdateAsync(freelancer);

            // Invalidate cache for this freelancer
            _memoryCache.Remove($"Freelancer_{id}");

            return NoContent();
        } catch (DbUpdateException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = StatusCodes.Status500InternalServerError,
                message = "Database error occurred while archiving the freelancer.",
                details = ex.InnerException?.Message ?? ex.Message
            });
        } catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = StatusCodes.Status500InternalServerError,
                message = "An error occurred while archiving the freelancer.",
                details = ex.Message
            });
        }
    }
    [HttpPut("{id}/unarchive")]
    public async Task<IActionResult> UnarchiveFreelancer(int id)
    {
        try
        {
            var freelancer = await _freelancerRepository.GetByIdAsync(id);
            if (freelancer == null)
                return NotFound(new
                {
                    status = StatusCodes.Status404NotFound,
                    message = $"Freelancer with ID {id} not found."
                });

            freelancer.IsArchived = false;
            await _freelancerRepository.UpdateAsync(freelancer);

            // Invalidate cache for this freelancer
            _memoryCache.Remove($"Freelancer_{id}");

            return NoContent();
        } catch (DbUpdateException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = StatusCodes.Status500InternalServerError,
                message = "Database error occurred while unarchiving the freelancer.",
                details = ex.InnerException?.Message ?? ex.Message
            });
        } catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                status = StatusCodes.Status500InternalServerError,
                message = "An error occurred while unarchiving the freelancer.",
                details = ex.Message
            });
        }
    }
    #endregion

    [HttpGet("generate-token")]
    [AllowAnonymous] // Allows access without authentication
    public IActionResult GenerateToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new { token = tokenString });
    }
}