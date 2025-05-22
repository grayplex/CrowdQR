using CrowdQR.Api.Services;

namespace CrowdQR.Api.Tests.Services;

/// <summary>
/// Unit tests for the TokenService.
/// </summary>
public class TokenServiceTests
{
    private readonly TokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenServiceTests"/> class.
    /// </summary>
    public TokenServiceTests()
    {
        _tokenService = new TokenService();
    }

    /// <summary>
    /// Tests that GenerateVerificationToken returns a non-empty token.
    /// </summary>
    [Fact]
    public void GenerateVerificationToken_ReturnsNonEmptyToken()
    {
        // Act
        var token = _tokenService.GenerateVerificationToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Length.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that GenerateVerificationToken returns unique tokens.
    /// </summary>
    [Fact]
    public void GenerateVerificationToken_ReturnsUniqueTokens()
    {
        // Act
        var token1 = _tokenService.GenerateVerificationToken();
        var token2 = _tokenService.GenerateVerificationToken();

        // Assert
        token1.Should().NotBe(token2);
        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tests that GenerateVerificationToken returns URL-safe tokens.
    /// </summary>
    [Fact]
    public void GenerateVerificationToken_ReturnsUrlSafeToken()
    {
        // Act
        var token = _tokenService.GenerateVerificationToken();

        // Assert
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");

        // Should only contain URL-safe Base64 characters
        token.Should().MatchRegex(@"^[A-Za-z0-9_-]+$");
    }

    /// <summary>
    /// Tests that GenerateVerificationToken produces tokens of reasonable length.
    /// </summary>
    [Fact]
    public void GenerateVerificationToken_ReturnsReasonableLength()
    {
        // Act
        var token = _tokenService.GenerateVerificationToken();

        // Assert
        // Base64 encoding of 64 random bytes should produce ~85 characters
        // After URL-safe replacements, it should be similar length
        token.Length.Should().BeGreaterThan(80);
        token.Length.Should().BeLessThan(100);
    }

    /// <summary>
    /// Tests that GenerateTokenExpiry with default hours returns future time.
    /// </summary>
    [Fact]
    public void GenerateTokenExpiry_DefaultHours_ReturnsFutureTime()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var expiry = _tokenService.GenerateTokenExpiry();

        // Assert
        var afterCall = DateTime.UtcNow;

        expiry.Should().BeAfter(beforeCall.AddHours(23)); // Should be ~24 hours from now
        expiry.Should().BeBefore(afterCall.AddHours(25)); // Allow some tolerance
    }

    /// <summary>
    /// Tests that GenerateTokenExpiry with custom hours works correctly.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(48)]
    [InlineData(168)] // 1 week
    public void GenerateTokenExpiry_CustomHours_ReturnsCorrectTime(int hours)
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var expiry = _tokenService.GenerateTokenExpiry(hours);

        // Assert
        var afterCall = DateTime.UtcNow;

        expiry.Should().BeAfter(beforeCall.AddHours(hours - 0.1)); // Small tolerance
        expiry.Should().BeBefore(afterCall.AddHours(hours + 0.1));
    }

    /// <summary>
    /// Tests that GenerateTokenExpiry with zero hours returns current time.
    /// </summary>
    [Fact]
    public void GenerateTokenExpiry_ZeroHours_ReturnsCurrentTime()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var expiry = _tokenService.GenerateTokenExpiry(0);

        // Assert
        var afterCall = DateTime.UtcNow;

        expiry.Should().BeAfter(beforeCall.AddSeconds(-1)); // Small tolerance
        expiry.Should().BeBefore(afterCall.AddSeconds(1));
    }

    /// <summary>
    /// Tests that GenerateTokenExpiry with negative hours returns past time.
    /// </summary>
    [Fact]
    public void GenerateTokenExpiry_NegativeHours_ReturnsPastTime()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var expiry = _tokenService.GenerateTokenExpiry(-1);

        // Assert
        expiry.Should().BeBefore(beforeCall);
        expiry.Should().BeAfter(beforeCall.AddHours(-2)); // Should be ~1 hour ago
    }

    /// <summary>
    /// Tests that multiple calls to GenerateTokenExpiry with same hours return similar times.
    /// </summary>
    [Fact]
    public void GenerateTokenExpiry_MultipleCalls_ReturnsSimilarTimes()
    {
        // Act
        var expiry1 = _tokenService.GenerateTokenExpiry(24);
        var expiry2 = _tokenService.GenerateTokenExpiry(24);

        // Assert
        var timeDifference = Math.Abs((expiry1 - expiry2).TotalSeconds);
        timeDifference.Should().BeLessThan(1); // Should be within 1 second of each other
    }

    /// <summary>
    /// Tests that GenerateVerificationToken produces tokens with sufficient entropy.
    /// </summary>
    [Fact]
    public void GenerateVerificationToken_HasSufficientEntropy()
    {
        // Act - Generate multiple tokens
        var tokens = new HashSet<string>();
        for (int i = 0; i < 1000; i++)
        {
            tokens.Add(_tokenService.GenerateVerificationToken());
        }

        // Assert - All tokens should be unique (extremely high probability)
        tokens.Count.Should().Be(1000);
    }

    /// <summary>
    /// Tests that GenerateVerificationToken doesn't contain common patterns.
    /// </summary>
    [Fact]
    public void GenerateVerificationToken_NoCommonPatterns()
    {
        // Act
        var token = _tokenService.GenerateVerificationToken();

        // Assert
        // Should not contain repetitive patterns
        token.Should().NotMatchRegex(@"(.)\1{4,}"); // No character repeated 5+ times in a row
        token.Should().NotContain("00000");
        token.Should().NotContain("11111");
        token.Should().NotContain("aaaaa");
        token.Should().NotContain("AAAAA");
    }

    /// <summary>
    /// Tests the consistency of token generation over multiple calls.
    /// </summary>
    [Fact]
    public void GenerateVerificationToken_ConsistentFormat()
    {
        // Act - Generate multiple tokens
        var tokens = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            tokens.Add(_tokenService.GenerateVerificationToken());
        }

        // Assert - All tokens should have similar characteristics
        foreach (var token in tokens)
        {
            token.Should().NotBeNullOrEmpty();
            token.Should().MatchRegex(@"^[A-Za-z0-9_-]+$"); // URL-safe Base64
            token.Length.Should().BeInRange(80, 100); // Reasonable length range
        }

        // All tokens should be unique
        tokens.Distinct().Count().Should().Be(tokens.Count);
    }

    /// <summary>
    /// Tests that GenerateTokenExpiry handles edge cases.
    /// </summary>
    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void GenerateTokenExpiry_EdgeCases_HandlesGracefully(int hours)
    {
        // Act & Assert - Should not throw exception
        var expiry = _tokenService.GenerateTokenExpiry(hours);

        // Should return a valid DateTime
        expiry.Should().NotBe(default);
    }
}