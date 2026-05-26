using InTheLoop.Api.Config;
using InTheLoop.Api.Models;
using InTheLoop.Api.Services;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Tests.Services;

public class JwtServiceTests
{
    private IConfiguration CreateConfig(string secret = "TestSecretKey12345678901234567890", string issuer = "InTheLoop.Api", string audience = "InTheLoop.Web", int expiryMinutes = 60)
    {
        var config = new Dictionary<string, string>
        {
            ["Jwt:Secret"] = secret,
            ["Jwt:Issuer"] = issuer,
            ["Jwt:Audience"] = audience,
            ["Jwt:ExpiryMinutes"] = expiryMinutes.ToString()
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();
    }

    [Fact]
    public void GenerateToken_ReturnsNonEmptyToken()
    {
        // Arrange
        var config = CreateConfig();
        var service = new JwtService(config);
        var user = new User { Id = "test-user-1", Email = "test@example.com", FirstName = "Test", LastName = "User" };

        // Act
        var token = service.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        // Arrange
        var config = CreateConfig();
        var service = new JwtService(config);
        var user = new User { Id = "test-user-2", Email = "user@example.com", FirstName = "First", LastName = "Last" };

        // Act
        var token = service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub");
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email");
        var issuerClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "iss");
        var audienceClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "aud");

        Assert.NotNull(subClaim);
        Assert.Equal("test-user-2", subClaim.Value);
        Assert.NotNull(emailClaim);
        Assert.Equal("user@example.com", emailClaim.Value);
        Assert.NotNull(issuerClaim);
        Assert.Equal("InTheLoop.Api", issuerClaim.Value);
        Assert.NotNull(audienceClaim);
        Assert.Equal("InTheLoop.Web", audienceClaim.Value);
    }

    [Fact]
    public void GenerateToken_ContainsJtiClaim()
    {
        // Arrange
        var config = CreateConfig();
        var service = new JwtService(config);
        var user = new User { Id = "test-user-3", Email = "jti@example.com" };

        // Act
        var token = service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "jti");
        Assert.NotNull(jtiClaim);
        Assert.NotEmpty(jtiClaim.Value);
    }

    [Fact]
    public void GenerateToken_IsValidJwtFormat()
    {
        // Arrange
        var config = CreateConfig();
        var service = new JwtService(config);
        var user = new User { Id = "test-user-4", Email = "format@example.com" };

        // Act
        var token = service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();

        // Assert
        Assert.True(handler.CanReadToken(token));
        var jwtToken = handler.ReadJwtToken(token);
        Assert.NotNull(jwtToken);
    }

    [Fact]
    public void GenerateToken_ContainsExpiryTime()
    {
        // Arrange
        var config = CreateConfig(expiryMinutes: 60);
        var service = new JwtService(config);
        var user = new User { Id = "test-user-5", Email = "expiry@example.com" };

        // Act
        var token = service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
        Assert.NotNull(expClaim);
        var expTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim.Value));
        Assert.True(expTime > DateTime.UtcNow.AddMinutes(55));
        Assert.True(expTime < DateTime.UtcNow.AddMinutes(65));
    }

    [Fact]
    public void GenerateToken_UsesDefaultExpiryMinutes()
    {
        // Arrange
        var config = new Dictionary<string, string>
        {
            ["Jwt:Secret"] = "TestSecretKey12345678901234567890",
            ["Jwt:Issuer"] = "InTheLoop.Api",
            ["Jwt:Audience"] = "InTheLoop.Web"
            // ExpiryMinutes not set, should default to 60
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();
        var service = new JwtService(configuration);
        var user = new User { Id = "test-user-6", Email = "default-expiry@example.com" };

        // Act
        var token = service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
        Assert.NotNull(expClaim);
        var expTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim.Value));
        Assert.True(expTime > DateTime.UtcNow.AddMinutes(55));
        Assert.True(expTime < DateTime.UtcNow.AddMinutes(65));
    }
}
