namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Repositories;
using Moq;
using Xunit;

public sealed class OwnershipGuardTests
{
    private readonly Mock<ICurrentUserAccessor> _userAccessorMock = new();
    private readonly Mock<IPersonProfileRepository> _profileRepositoryMock = new();
    private readonly OwnershipGuard _sut;

    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid ProfileId = Guid.NewGuid();

    public OwnershipGuardTests()
    {
        _sut = new OwnershipGuard(_userAccessorMock.Object, _profileRepositoryMock.Object);
    }

    // ─── CurrentUserId ────────────────────────────────────────────────────────

    [Fact]
    public void CurrentUserId_DelegatesToCurrentUserAccessor()
    {
        _userAccessorMock.Setup(x => x.GetCurrentUserId()).Returns(OwnerId);

        Assert.Equal(OwnerId, _sut.CurrentUserId);
    }

    // ─── GetOwnedProfileAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetOwnedProfileAsync_ReturnsProfile_WhenProfileExists()
    {
        var profile = new PersonProfile { Id = ProfileId, OwnerId = OwnerId };
        _userAccessorMock.Setup(x => x.GetCurrentUserId()).Returns(OwnerId);
        _profileRepositoryMock
            .Setup(x => x.GetOwnedByIdAsync(ProfileId, OwnerId, default))
            .ReturnsAsync(profile);

        var result = await _sut.GetOwnedProfileAsync(ProfileId);

        Assert.Equal(ProfileId, result.Id);
    }

    [Fact]
    public async Task GetOwnedProfileAsync_ThrowsInvalidOperation_WhenProfileNotFound()
    {
        _userAccessorMock.Setup(x => x.GetCurrentUserId()).Returns(OwnerId);
        _profileRepositoryMock
            .Setup(x => x.GetOwnedByIdAsync(ProfileId, OwnerId, default))
            .ReturnsAsync((PersonProfile?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetOwnedProfileAsync(ProfileId));

        Assert.Contains(ProfileId.ToString(), ex.Message);
    }

    [Fact]
    public async Task GetOwnedProfileAsync_UsesCurrentUserIdFromAccessor()
    {
        var differentOwner = Guid.NewGuid();
        _userAccessorMock.Setup(x => x.GetCurrentUserId()).Returns(differentOwner);
        _profileRepositoryMock
            .Setup(x => x.GetOwnedByIdAsync(ProfileId, differentOwner, default))
            .ReturnsAsync((PersonProfile?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetOwnedProfileAsync(ProfileId));

        _profileRepositoryMock.Verify(
            x => x.GetOwnedByIdAsync(ProfileId, differentOwner, default),
            Times.Once);
    }

    // ─── EnsureProfileOwnedAsync ──────────────────────────────────────────────

    [Fact]
    public async Task EnsureProfileOwnedAsync_DoesNotThrow_WhenProfileExists()
    {
        var profile = new PersonProfile { Id = ProfileId, OwnerId = OwnerId };
        _userAccessorMock.Setup(x => x.GetCurrentUserId()).Returns(OwnerId);
        _profileRepositoryMock
            .Setup(x => x.GetOwnedByIdAsync(ProfileId, OwnerId, default))
            .ReturnsAsync(profile);

        // Should complete without throwing
        await _sut.EnsureProfileOwnedAsync(ProfileId);

        _profileRepositoryMock.Verify(
            x => x.GetOwnedByIdAsync(ProfileId, OwnerId, default),
            Times.Once);
    }

    [Fact]
    public async Task EnsureProfileOwnedAsync_Throws_WhenProfileNotFound()
    {
        _userAccessorMock.Setup(x => x.GetCurrentUserId()).Returns(OwnerId);
        _profileRepositoryMock
            .Setup(x => x.GetOwnedByIdAsync(ProfileId, OwnerId, default))
            .ReturnsAsync((PersonProfile?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.EnsureProfileOwnedAsync(ProfileId));
    }

    [Fact]
    public async Task EnsureProfileOwnedAsync_PassesCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var profile = new PersonProfile { Id = ProfileId, OwnerId = OwnerId };

        _userAccessorMock.Setup(x => x.GetCurrentUserId()).Returns(OwnerId);
        _profileRepositoryMock
            .Setup(x => x.GetOwnedByIdAsync(ProfileId, OwnerId, token))
            .ReturnsAsync(profile);

        await _sut.EnsureProfileOwnedAsync(ProfileId, token);

        _profileRepositoryMock.Verify(
            x => x.GetOwnedByIdAsync(ProfileId, OwnerId, token),
            Times.Once);
    }
}
