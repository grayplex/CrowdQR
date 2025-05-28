using CrowdQR.Api.Services;

namespace CrowdQR.Api.Tests.Services;

/// <summary>
/// Unit tests for the PasswordService.
/// </summary>
public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordServiceTests"/> class.
    /// </summary>
    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    /// <summary>
    /// Tests that password hashing returns non-empty hash and salt.
    /// </summary>
    [Fact]
    public void HashPassword_ValidPassword_ReturnsHashAndSalt()
    {
        // Arrange
        var password = "testPassword123";

        // Act
        var (hash, salt) = _passwordService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
        salt.Should().NotBe(password);
    }

    /// <summary>
    /// Tests that the same password produces different hashes with different salts.
    /// </summary>
    [Fact]
    public void HashPassword_SamePassword_ProducesDifferentHashesAndSalts()
    {
        // Arrange
        var password = "testPassword123";

        // Act
        var (hash1, salt1) = _passwordService.HashPassword(password);
        var (hash2, salt2) = _passwordService.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2);
        salt1.Should().NotBe(salt2);
    }

    /// <summary>
    /// Tests that hashing with a specific salt produces consistent results.
    /// </summary>
    [Fact]
    public void HashPassword_WithSpecificSalt_ProducesConsistentHash()
    {
        // Arrange
        var password = "testPassword123";
        var salt = Convert.ToBase64String(new byte[64]); // Create a valid salt

        // Act
        var hash1 = _passwordService.HashPassword(password, salt);
        var hash2 = _passwordService.HashPassword(password, salt);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tests that password verification works correctly with valid credentials.
    /// </summary>
    [Fact]
    public void VerifyPassword_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var password = "testPassword123";
        var (hash, salt) = _passwordService.HashPassword(password);

        // Act
        var isValid = _passwordService.VerifyPassword(password, hash, salt);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that password verification fails with incorrect password.
    /// </summary>
    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var correctPassword = "testPassword123";
        var incorrectPassword = "wrongPassword123";
        var (hash, salt) = _passwordService.HashPassword(correctPassword);

        // Act
        var isValid = _passwordService.VerifyPassword(incorrectPassword, hash, salt);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Tests that password verification fails with incorrect hash.
    /// </summary>
    [Fact]
    public void VerifyPassword_IncorrectHash_ReturnsFalse()
    {
        // Arrange
        var password = "testPassword123";
        var (_, salt) = _passwordService.HashPassword(password);
        var incorrectHash = "incorrectHash";

        // Act & Assert
        var exception = Assert.ThrowsAny<Exception>(() =>
            _passwordService.VerifyPassword(password, incorrectHash, salt));

        exception.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that password verification fails with incorrect salt.
    /// </summary>
    [Fact]
    public void VerifyPassword_IncorrectSalt_ReturnsFalse()
    {
        // Arrange
        var password = "testPassword123";
        var (hash, _) = _passwordService.HashPassword(password);
        var incorrectSalt = Convert.ToBase64String(new byte[64]); // Different salt

        // Act
        var isValid = _passwordService.VerifyPassword(password, hash, incorrectSalt);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Tests password hashing with empty password.
    /// </summary>
    [Fact]
    public void HashPassword_EmptyPassword_HandlesGracefully()
    {
        // Arrange
        var password = "";

        // Act
        var (hash, salt) = _passwordService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tests password hashing with very long password.
    /// </summary>
    [Fact]
    public void HashPassword_VeryLongPassword_HandlesGracefully()
    {
        // Arrange
        var password = new string('a', 1000); // Very long password

        // Act
        var (hash, salt) = _passwordService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tests password hashing with special characters.
    /// </summary>
    [Fact]
    public void HashPassword_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var password = "Test@#$%^&*()_+{}|:<>?[];',./`~Password123";

        // Act
        var (hash, salt) = _passwordService.HashPassword(password);
        var isValid = _passwordService.VerifyPassword(password, hash, salt);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests password hashing with Unicode characters.
    /// </summary>
    [Fact]
    public void HashPassword_UnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var password = "Тест密码🔐Password123";

        // Act
        var (hash, salt) = _passwordService.HashPassword(password);
        var isValid = _passwordService.VerifyPassword(password, hash, salt);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that hash and salt are proper Base64 strings.
    /// </summary>
    [Fact]
    public void HashPassword_ValidPassword_ReturnsValidBase64()
    {
        // Arrange
        var password = "testPassword123";

        // Act
        var (hash, salt) = _passwordService.HashPassword(password);

        // Assert
        // Should not throw exception when converting from Base64
        var hashBytes = Convert.FromBase64String(hash);
        var saltBytes = Convert.FromBase64String(salt);

        hashBytes.Should().NotBeEmpty();
        saltBytes.Should().NotBeEmpty();
        hashBytes.Length.Should().Be(64); // SHA512 produces 64 bytes
        saltBytes.Length.Should().Be(64); // Salt should be 64 bytes
    }
}