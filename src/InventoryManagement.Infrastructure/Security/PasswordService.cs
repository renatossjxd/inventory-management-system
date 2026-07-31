using System.Security.Cryptography;
using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Infrastructure.Security;

public sealed class PasswordService : IPasswordService
{
    private const int Iterations = 100_000;
    private const int KeySize = 32;

    public (string Hash, string Salt) Hash(string password)
    {
        if (password.Length < 8) throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.", nameof(password));
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string hash, string salt)
    {
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(salt), Iterations,
            HashAlgorithmName.SHA256, KeySize);
        return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(hash));
    }
}
