using Microsoft.AspNetCore.Mvc;
using RpFlo.Application.DTOs;
using RpFlo.Application.Interfaces;

namespace RpFlo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController(IUserRepository userRepo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(CancellationToken ct)
    {
        var users = await userRepo.GetAllAsync(ct);
        var response = users
            .Select(u => new UserResponse(u.Id, u.Name, u.Email, u.Role.ToString(), u.Department.ToString()))
            .ToList();
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken ct)
    {
        var user = await userRepo.GetByIdAsync(id, ct);
        if (user is null) return NotFound();
        return Ok(new UserResponse(user.Id, user.Name, user.Email, user.Role.ToString(), user.Department.ToString()));
    }
}
