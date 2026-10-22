using AppRoulette.Models;
using AppRoulette.Services;

namespace AppRoulette.Tests.Services;

/// <summary>
/// <see cref="SyncConflictResolver"/> のユニットテストクラスです。
/// </summary>
public class SyncConflictResolverTests
{
    private static SyncGroupData CreateData(
        int groupId, DateTime lastModifiedUtc)
    {
        return new SyncGroupData
        {
            GroupId = groupId,
            DisplayName = $"グループ{groupId}",
            LastModifiedUtc = lastModifiedUtc,
        };
    }

    // ---------------------------------------------------------------
    // ChooseNewer
    // ---------------------------------------------------------------

    [Fact]
    public void ChooseNewer_リモートが新しい場合_リモートを返す()
    {
        // Arrange
        var local = CreateData(1, new DateTime(2026, 1, 1, 0, 0, 0));
        var remote = CreateData(1, new DateTime(2026, 1, 2, 0, 0, 0));

        // Act
        var result = SyncConflictResolver.ChooseNewer(local, remote);

        // Assert
        Assert.Same(remote, result);
    }

    [Fact]
    public void ChooseNewer_ローカルが新しい場合_ローカルを返す()
    {
        // Arrange
        var local = CreateData(1, new DateTime(2026, 1, 2, 0, 0, 0));
        var remote = CreateData(1, new DateTime(2026, 1, 1, 0, 0, 0));

        // Act
        var result = SyncConflictResolver.ChooseNewer(local, remote);

        // Assert
        Assert.Same(local, result);
    }

    [Fact]
    public void ChooseNewer_同時刻の場合_ローカルを優先する()
    {
        // Arrange
        var timestamp = new DateTime(2026, 1, 1, 0, 0, 0);
        var local = CreateData(1, timestamp);
        var remote = CreateData(1, timestamp);

        // Act
        var result = SyncConflictResolver.ChooseNewer(local, remote);

        // Assert
        Assert.Same(local, result);
    }

    [Fact]
    public void ChooseNewer_ローカルがnullの場合_リモートを返す()
    {
        // Arrange
        var remote = CreateData(1, new DateTime(2026, 1, 1, 0, 0, 0));

        // Act
        var result = SyncConflictResolver.ChooseNewer(null, remote);

        // Assert
        Assert.Same(remote, result);
    }

    [Fact]
    public void ChooseNewer_リモートがnullの場合_ローカルを返す()
    {
        // Arrange
        var local = CreateData(1, new DateTime(2026, 1, 1, 0, 0, 0));

        // Act
        var result = SyncConflictResolver.ChooseNewer(local, null);

        // Assert
        Assert.Same(local, result);
    }

    [Fact]
    public void ChooseNewer_両方nullの場合_nullを返す()
    {
        // Act
        var result = SyncConflictResolver.ChooseNewer(null, null);

        // Assert
        Assert.Null(result);
    }

    // ---------------------------------------------------------------
    // ShouldApplyRemote
    // ---------------------------------------------------------------

    [Fact]
    public void ShouldApplyRemote_リモートが新しい場合_trueを返す()
    {
        // Arrange
        var local = CreateData(1, new DateTime(2026, 1, 1, 0, 0, 0));
        var remote = CreateData(1, new DateTime(2026, 1, 2, 0, 0, 0));

        // Act & Assert
        Assert.True(SyncConflictResolver.ShouldApplyRemote(local, remote));
    }

    [Fact]
    public void ShouldApplyRemote_ローカルが新しい場合_falseを返す()
    {
        // Arrange
        var local = CreateData(1, new DateTime(2026, 1, 2, 0, 0, 0));
        var remote = CreateData(1, new DateTime(2026, 1, 1, 0, 0, 0));

        // Act & Assert
        Assert.False(SyncConflictResolver.ShouldApplyRemote(local, remote));
    }

    [Fact]
    public void ShouldApplyRemote_同時刻の場合_falseを返す()
    {
        // Arrange
        var timestamp = new DateTime(2026, 1, 1, 0, 0, 0);
        var local = CreateData(1, timestamp);
        var remote = CreateData(1, timestamp);

        // Act & Assert
        Assert.False(SyncConflictResolver.ShouldApplyRemote(local, remote));
    }

    [Fact]
    public void ShouldApplyRemote_リモートがnullの場合_falseを返す()
    {
        // Arrange
        var local = CreateData(1, new DateTime(2026, 1, 1, 0, 0, 0));

        // Act & Assert
        Assert.False(SyncConflictResolver.ShouldApplyRemote(local, null));
    }

    [Fact]
    public void ShouldApplyRemote_ローカルがnullでリモートがある場合_trueを返す()
    {
        // Arrange
        var remote = CreateData(1, new DateTime(2026, 1, 1, 0, 0, 0));

        // Act & Assert
        Assert.True(SyncConflictResolver.ShouldApplyRemote(null, remote));
    }
}
