﻿namespace IdentityService.Interfaces
{
    public interface IIdentityService
    {
        Task<ITokenResponse?> LoginAsync(string username, string password, CancellationToken cancellationToken);
        Task<ITokenResponse?> LoginJwtAsync(string username, string password, CancellationToken cancellationToken);
        Task<ITokenResponse?> RefreshToken(string refreshToken, CancellationToken cancellationToken);
        Task<bool> SignoutAsync(string accessToken, CancellationToken cancellation);
    }
}
