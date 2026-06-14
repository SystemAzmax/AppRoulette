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
        var fakeRepo = new FakeItemRepository();
        var sut = new MainViewModel(
            new FakeRandomService(0),
            fakeRepo);
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
        Assert.Empty(sut.SelectedGroup.Items);

        // グループ1に戻ると、正しくアイテムが復元される
        sut.SelectedGroup = sut.GroupList[0];
        Assert.Equal(2, sut.ItemCount);
        Assert.Equal("グループ1のアイテムA,1\nグループ1のアイテムB,1", sut.ItemsText);
        Assert.Equal("グループ1のアイテムA", sut.SelectedGroup.Items[0].Name);
        Assert.Equal("グループ1のアイテムB", sut.SelectedGroup.Items[1].Name);

        // グループ2に戻ると、正しくアイテムが復元される
        sut.SelectedGroup = sut.GroupList[1];
        Assert.Equal(3, sut.ItemCount);
        Assert.Equal("グループ2のアイテムX,1\nグループ2のアイテムY,1\nグループ2のアイテムZ,1", sut.ItemsText);
        Assert.Equal("グループ2のアイテムX", sut.SelectedGroup.Items[0].Name);
        Assert.Equal("グループ2のアイテムY", sut.SelectedGroup.Items[1].Name);
        Assert.Equal("グループ2のアイテムZ", sut.SelectedGroup.Items[2].Name);

        // 非同期保存の完了を待つ
        await Task.Delay(100);

        // SQLite にそれぞれのグループアイテムが保存されているはず（テスト簡略化）
        // 詳細な検証は FakeItemRepository を直接確認するか、統合テストで行う
    }
}
