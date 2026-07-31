using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Models;
using InventoryManagement.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IInventoryDbContext db, IPasswordService passwords, ITokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var hasUsers = await db.Users.AnyAsync(ct);
        if (hasUsers && !User.IsInRole(UserRoles.Admin)) return Forbid();
        if (await db.Users.AnyAsync(x => x.Email == request.Email.ToLower(), ct))
            return Conflict(new ProblemDetails { Title = "El correo ya está registrado." });
        var (hash, salt) = passwords.Hash(request.Password);
        var role = hasUsers ? UserRoles.Normalize(request.Role) : UserRoles.Admin;
        var user = new AppUser(request.Email, request.DisplayName, hash, salt, role);
        db.AddUser(user);
        await db.SaveChangesAsync(ct);
        return Created("/api/auth/me", new AuthResponse(tokens.Create(user)));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == request.Email.ToLower(), ct);
        if (user is null || !passwords.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized(new ProblemDetails { Title = "Credenciales inválidas." });
        return Ok(new AuthResponse(tokens.Create(user)));
    }

    [HttpGet("users")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetUsers(CancellationToken ct) => Ok(await db.Users
        .AsNoTracking().OrderBy(x => x.DisplayName)
        .Select(x => new UserResponse(x.Id, x.Email, x.DisplayName, x.Role, x.CreatedAtUtc)).ToListAsync(ct));
}
