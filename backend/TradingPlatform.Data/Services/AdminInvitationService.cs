
using System.Security.Cryptography;
using System.Text;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Services;

/// <summary>
/// Service for managing admin invitation tokens
/// Handles generation, validation, and tracking of admin invitation links
/// </summary>
public sealed class AdminInvitationService : IAdminInvitationService
{
    private readonly IAdminInvitationRepository _repository;
    private const int TokenLength = 32;

    public AdminInvitationService(IAdminInvitationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Generate invitation token for new admin
    /// Token is valid for 48 hours, one-time use
    /// </summary>
    public async Task<string> GenerateInvitationAsync(
        string email,
        string firstName,
        string lastName,
        Guid invitedBy,
        int expiryHours = 48,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));

        // Check if invitation already exists for this email
        var existingInvite = await _repository.GetByEmailAsync(email, cancellationToken);
        if (existingInvite != null && existingInvite.Status == AdminInvitationStatus.Pending && !IsExpired(existingInvite))
        {
            throw new InvalidOperationException($"Active invitation already exists for {email}");
        }

        try
        {
            // Generate cryptographically strong random token
            string token = GenerateRandomToken();

            var invitation = new AdminInvitationEntity
            {
                Id = Guid.NewGuid(),
                Token = token,
                Email = email.ToLower(),
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                InvitedBy = invitedBy,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(expiryHours),
                Status = AdminInvitationStatus.Pending
            };

            await _repository.AddAsync(invitation, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return token;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate invitation", ex);
        }
    }

    /// <summary>
    /// Validate invitation token (must exist, not expired, not used, not revoked)
    /// </summary>
    public async Task<AdminInvitationEntity> ValidateInvitationAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required", nameof(token));

        try
        {
            var invitation = await _repository.GetByTokenAsync(token.Trim(), cancellationToken);

            if (invitation == null)
                throw new InvalidOperationException("Invitation token not found");

            // Check if already used
            if (invitation.Status == AdminInvitationStatus.Used)
                throw new InvalidOperationException("Invitation has already been used");

            // Check if revoked
            if (invitation.Status == AdminInvitationStatus.Revoked)
                throw new InvalidOperationException("Invitation has been revoked");

            // Check if expired
            if (IsExpired(invitation))
            {
                // Mark as expired if it isn't already
                if (invitation.Status != AdminInvitationStatus.Expired)
                {
                    await MarkAsExpiredAsync(invitation.Id, cancellationToken);
                }
                throw new InvalidOperationException("Invitation has expired");
            }

            return invitation;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to validate invitation", ex);
        }
    }

    /// <summary>
    /// Mark invitation as used (when admin successfully registers)
    /// </summary>
    public async Task MarkAsUsedAsync(
        string token,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required", nameof(token));

        try
        {
            var invitation = await _repository.GetByTokenAsync(token.Trim(), cancellationToken);
            if (invitation == null)
                throw new InvalidOperationException("Invitation not found");

            invitation.UsedAt = DateTimeOffset.UtcNow;
            invitation.UsedBy = adminId;
            invitation.Status = AdminInvitationStatus.Used;

            await _repository.UpdateAsync(invitation, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to mark invitation as used", ex);
        }
    }

    /// <summary>
    /// Revoke an invitation (typically by Super Admin)
    /// </summary>
    public async Task RevokeAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var invitation = await _repository.GetByIdAsync(invitationId, cancellationToken);
            if (invitation == null)
                throw new InvalidOperationException("Invitation not found");

            invitation.Status = AdminInvitationStatus.Revoked;

            await _repository.UpdateAsync(invitation, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to revoke invitation", ex);
        }
    }

    /// <summary>
    /// Clean up expired invitations (typically called by background job)
    /// </summary>
    public async Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredInvitations = await _repository.GetExpiredInvitationsAsync(cancellationToken);
            
            foreach (var invitation in expiredInvitations)
            {
                if (invitation.Status != AdminInvitationStatus.Expired)
                {
                    invitation.Status = AdminInvitationStatus.Expired;
                    await _repository.UpdateAsync(invitation, cancellationToken);
                }
            }

            if (expiredInvitations.Any())
            {
                await _repository.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to cleanup expired invitations", ex);
        }
    }

    /// <summary>
    /// Check if invitation is expired
    /// </summary>
    private static bool IsExpired(AdminInvitationEntity invitation)
    {
        return DateTimeOffset.UtcNow > invitation.ExpiresAt;
    }

    /// <summary>
    /// Mark invitation as expired
    /// </summary>
    private async Task MarkAsExpiredAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var invitation = await _repository.GetByIdAsync(invitationId, cancellationToken);
        if (invitation != null)
        {
            invitation.Status = AdminInvitationStatus.Expired;
            await _repository.UpdateAsync(invitation, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Generate cryptographically strong random token
    /// </summary>
    private static string GenerateRandomToken()
    {
        const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghjkmnopqrstuvwxyz0123456789";
        var sb = new System.Text.StringBuilder(TokenLength);

        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            byte[] randomBytes = new byte[TokenLength];
            rng.GetBytes(randomBytes);

            for (int i = 0; i < TokenLength; i++)
            {
                sb.Append(validChars[randomBytes[i] % validChars.Length]);
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// Interface for admin invitation service
/// </summary>
public interface IAdminInvitationService
{
    /// <summary>Generate invitation token</summary>
    Task<string> GenerateInvitationAsync(
        string email,
        string firstName,
        string lastName,
        Guid invitedBy,
        int expiryHours = 48,
        CancellationToken cancellationToken = default);

    /// <summary>Validate invitation token</summary>
    Task<AdminInvitationEntity> ValidateInvitationAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>Mark invitation as used</summary>
    Task MarkAsUsedAsync(
        string token,
        Guid adminId,
        CancellationToken cancellationToken = default);

    /// <summary>Revoke invitation</summary>
    Task RevokeAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default);

    /// <summary>Cleanup expired invitations</summary>
    Task CleanupExpiredAsync(CancellationToken cancellationToken = default);
}
