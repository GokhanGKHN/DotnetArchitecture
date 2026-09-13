using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.Domain.Entities;
using MediatR;

namespace DotnetArchitecture.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. E-posta adresi daha önce alınmış mı kontrol et
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new InvalidOperationException("Bu e-posta adresiyle kayıtlı bir kullanıcı zaten mevcut.");
        }

        // 2. Şifreyi tuzlayarak (salt) güvenle hash'le
        var passwordHash = _passwordHasher.Hash(request.Password);

        // 3. Domain Entity'sini oluştur
        var user = User.Create(
            email: request.Email,
            firstName: request.FirstName,
            lastName: request.LastName,
            passwordHash: passwordHash,
            role: request.Role);

        // 4. Veritabanına kaydet
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Giriş yapabilmesi için JWT Token üret
        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResponse(
            Token: token,
            UserId: user.Id,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role.ToString(),
            ExpiresAtUtc: expiresAt);
    }
}
