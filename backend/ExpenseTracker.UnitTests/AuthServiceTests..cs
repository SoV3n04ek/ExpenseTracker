using ExpenseTracker.Application.Configuration;
using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Application.Services;
using ExpenseTracker.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ExpenseTracker.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserMgr;
    private readonly AuthService _authService;
    private readonly Mock<IEmailService> _mockEmailService;

    public AuthServiceTests()
    {
        _mockUserMgr = MockHelpers.MockUserManager<ApplicationUser>();
        _mockEmailService = new Mock<IEmailService>();

        // 1. Create a real instance of your settings class
        var jwtSettings = new JwtSettings
        {
            Key = "SuperSecretTestKey1234567890123456",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpireMinutes = 60
        };

        // 2. Use Options.Create to wrap your settings
        var options = Options.Create(jwtSettings);

        // 3. Pass the options to the service
        _authService = new AuthService(_mockUserMgr.Object, options, _mockEmailService.Object);
    }

    [Fact] // --- TEST A: Valid credentials return a token ---
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var user = new ApplicationUser { Id = 1, Email = "test@test.com", Name = "Tester" };
        var loginDto = new LoginDto { Email = "test@test.com", Password = "Password123!" };

        _mockUserMgr.Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);
        _mockUserMgr.Setup(x => x.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(true);
        _mockUserMgr.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result.Token);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.Id.ToString(), result.UserId);
    }

    [Fact] // --- Test B: Invalid password throws exception ---
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@test.com" };
        var loginDto = new LoginDto { Email = "test@test.com", Password = "wrong-password" };

        _mockUserMgr.Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _mockUserMgr.Setup(x => x.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(loginDto));
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ThrowsException()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "existing@test.com",
            Password = "Password123!",
            Name = "New User"
        };

        // We simulate that CreateAsync fails because the user already exists
        _mockUserMgr.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email already taken " }));

        // Act & Assert 
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _authService.RegisterAsync(registerDto));

        Assert.Contains("Email already taken", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ThrowsUnauthorizedException()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "nonexistent@test.com", Password = "AnyPassword" };

        _mockUserMgr.Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync((ApplicationUser)null!); // Return null user

        // Act & Assert 
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _authService.LoginAsync(loginDto));
    }

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsAuthResponseAndCallsCreate()
    {
        // Arrange
        var dto = new RegisterDto { Email = "new@test.com", Password = "Password123!" };

        _mockUserMgr.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
                    .ReturnsAsync(IdentityResult.Success);

        _mockUserMgr.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
                    .ReturnsAsync("fake-token-123");

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        Assert.Equal("Please check your email to confirm your account.", result.Message);

        // Verify that email was sent
        _mockEmailService.Verify(x => x.SendEmailAsync(
            dto.Email,
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);

        _mockUserMgr.Verify(x => x.CreateAsync(
            It.Is<ApplicationUser>(u => u.Email == dto.Email),
            dto.Password), Times.Once);
    }

    [Fact]
    public async Task GenerateAuthResponse_ReturnsCorrectExpiration()
    {
        // Arrange
        var user = new ApplicationUser { Id = 1, Email = "test@test.com", Name = "Tester" };
        var loginDto = new LoginDto { Email = "test@test.com", Password = "Password123!" };

        _mockUserMgr.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockUserMgr.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);

        _mockUserMgr.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        // Check if expiration is roughly 60 minutes from now (allowing for 1-minute execution delay)
        var expectedExpiration = DateTime.Now.AddMinutes(60);
        Assert.True((result.Expiration - expectedExpiration).Duration() < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ConfirmEmail_ValidToken_ReturnsTrue()
    {
        // Arrange
        var user = new ApplicationUser { Id = 1, Email = "test@test.com" };
        _mockUserMgr.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
        _mockUserMgr.Setup(x => x.ConfirmEmailAsync(user, "valid-token"))
                    .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.ConfirmEmailAsync(1, "valid-token");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task LoginAsync_UnconfirmedEmail_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = new ApplicationUser { Email = "unconfirmed@test.com" };
        var loginDto = new LoginDto { Email = "unconfirmed@test.com", Password = "Password123!" };

        _mockUserMgr.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
        _mockUserMgr.Setup(x => x.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);
        _mockUserMgr.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(false); // <--- Mocking unconfirmed

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(loginDto));
        Assert.Equal("You must confirm your email before logging in.", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_ExistingUnconfirmedUser_ResendsEmail()
    {
        // Arrange
        var existingUser = new ApplicationUser { Email = "already@here.com", EmailConfirmed = false };
        var dto = new RegisterDto { Email = "already@here.com", Password = "Password123!", Name = "Retry" };

        _mockUserMgr.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(existingUser);
        _mockUserMgr.Setup(x => x.GenerateEmailConfirmationTokenAsync(existingUser)).ReturnsAsync("new-token");

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        Assert.Contains("uncorfimed", result.Message); // Verifying our custom logic message
        _mockEmailService.Verify(x => x.SendEmailAsync(dto.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}