using DotnetArchitecture.Application.Features.Auth.Commands.Register;
using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.Domain.Entities;
using DotnetArchitecture.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DotnetArchitecture.Application.UnitTests.Features.Auth;

public class RegisterCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new RegisterCommandHandler(
            _userRepository,
            _passwordHasher,
            _jwtTokenGenerator,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new RegisterCommand("existing@example.com", "Password123*", "Ali", "Veli");
        _userRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Bu e-posta adresiyle kayıtlı bir kullanıcı zaten mevcut.*");

        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldHashPasswordSaveUserAndReturnToken()
    {
        // Arrange
        var command = new RegisterCommand("newuser@example.com", "Password123*", "Ayşe", "Kaya", UserRole.Member);
        _userRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(false);
        _passwordHasher.Hash(command.Password)
            .Returns("hashed_secret_password");
        _jwtTokenGenerator.GenerateToken(Arg.Any<User>())
            .Returns(("jwt_mock_token", DateTime.UtcNow.AddHours(1)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("jwt_mock_token");
        result.Email.Should().Be("newuser@example.com");
        result.Role.Should().Be("Member");

        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == "newuser@example.com" && u.PasswordHash == "hashed_secret_password"),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
