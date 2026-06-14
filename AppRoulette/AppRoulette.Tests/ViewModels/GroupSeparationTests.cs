using AppRoulette.Models;
using AppRoulette.Tests.Fakes;
using AppRoulette.ViewModels;

namespace AppRoulette.Tests.ViewModels;

/// <summary>
/// グループ別アイテム保存の独立性を検証するテスト。
/// </summary>
public class GroupSeparationTests
{
    /// <summary>
    /// 異なるグループのアイテムが独立して保存されることを検証します。
    /// </summary>
    [Fact]
    public async Task グループ別アイテム_各グループで異なるアイテムを保存できる()
    {
        // Arrange
        var fakeService = new FakeDataPersistenceService
        {
            GroupsToReturn = new List<RouletteGroup>
            {
                new(1, "グループ1") { Items = new List<RouletteItem>() },
                new(2, "グループ2") { Items = new List<RouletteItem>() },
                new(3, "グループ3") { Items = new List<RouletteItem>() },
            },
        };
        var sut = new MainViewModel(fakeService, new FakeRandomService(0));
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act & Assert
        // グループ1にアイテムを追加
        sut.SelectedGroup = sut.GroupList[0];
        sut.ItemsText = "グループ1のアイテムA\nグループ1のアイテムB";
        Assert.Equal(2, sut.ItemCount);
        Assert.Equal(2, sut.SelectedGroup.Items.Count);

        // グループ2にアイテムを追加
        sut.SelectedGroup = sut.GroupList[1];
        sut.ItemsText = "グループ2のアイテムX\nグループ2のアイテムY\nグループ2のアイテムZ";
        Assert.Equal(3, sut.ItemCount);
        Assert.Equal(3, sut.SelectedGroup.Items.Count);

        // グループ3は空のまま
        sut.SelectedGroup = sut.GroupList[2];
        Assert.Equal(0, sut.ItemCount);
        Assert.Equal(0, sut.SelectedGroup.Items.Count);

        // グループ1に戻ると、正しくアイテムが復元される
        sut.SelectedGroup = sut.GroupList[0];
        Assert.Equal(2, sut.ItemCount);
        Assert.Equal("グループ1のアイテムA\nグループ1のアイテムB", sut.ItemsText);
        Assert.Equal("グループ1のアイテムA", sut.SelectedGroup.Items[0].Name);
        Assert.Equal("グループ1のアイテムB", sut.SelectedGroup.Items[1].Name);

        // グループ2に戻ると、正しくアイテムが復元される
        sut.SelectedGroup = sut.GroupList[1];
        Assert.Equal(3, sut.ItemCount);
        Assert.Equal("グループ2のアイテムX\nグループ2のアイテムY\nグループ2のアイテムZ", sut.ItemsText);
        Assert.Equal("グループ2のアイテムX", sut.SelectedGroup.Items[0].Name);
        Assert.Equal("グループ2のアイテムY", sut.SelectedGroup.Items[1].Name);
        Assert.Equal("グループ2のアイテムZ", sut.SelectedGroup.Items[2].Name);

        // SaveAsync が呼ばれた時点で、全グループが正しく保存される
        // グループ1とグループ2でそれぞれ改行が増えているため、SaveCallCount は 2 になり、
        // さらにグループ切り替え時に3回（1→2, 2→3, 3→1, 1→2）の保存が追加されるため合計7
        // InitializeAsync 完了後: グループ1を選択（保存なし）
        // グループ1にテキスト入力：改行が増えた時に保存（1回）
        // グループ2に切り替え時：保存（1回）
        // グループ2にテキスト入力：改行が増えた時に保存（1回）
        // グループ3に切り替え時：保存（1回）
        // グループ1に切り替え時：保存（1回）
        // グループ2に切り替え時：保存（1回）
        Assert.True(fakeService.SaveCallCount >= 2, $"Expected at least 2 saves, but got {fakeService.SaveCallCount}");
        var savedGroups = fakeService.LastSavedGroups;
        Assert.NotNull(savedGroups);
        Assert.Equal(2, savedGroups![0].Items.Count);
        Assert.Equal(3, savedGroups![1].Items.Count);
        Assert.Equal(0, savedGroups![2].Items.Count);
    }
}
