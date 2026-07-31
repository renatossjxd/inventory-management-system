using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Models;
using InventoryManagement.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IInventoryDbContext db, IPasswordService passwords, ITokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(x => x.Email == request.Email.ToLower(), ct))
            return Conflict(new ProblemDetails { Title = "El correo ya está registrado." });
        var (hash, salt) = passwords.Hash(request.Password);
        var user = new AppUser(request.Email, request.DisplayName, hash, salt);
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
}
