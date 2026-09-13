using DotnetArchitecture.Domain.Enums;
using MediatR;

namespace DotnetArchitecture.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role = UserRole.Member
) : IRequest<AuthResponse>;
