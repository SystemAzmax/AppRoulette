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
        var fakePersistence = new FakeDataPersistenceService();
        var sut = new MainViewModel(
            new FakeRandomService(0),
            fakeRepo,
            fakePersistence);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act & Assert
        // Roulette1にアイテムを追加
        sut.SelectedGroup = sut.GroupList[0];
        sut.ItemsText = "Roulette1のアイテムA\nRoulette1のアイテムB";
        Assert.Equal(2, sut.ItemCount);
        Assert.Equal(2, sut.SelectedGroup.Items.Count);

        // Roulette2にアイテムを追加
        sut.SelectedGroup = sut.GroupList[1];
        sut.ItemsText = "Roulette2のアイテムX\nRoulette2のアイテムY\nRoulette2のアイテムZ";
        Assert.Equal(3, sut.ItemCount);
        Assert.Equal(3, sut.SelectedGroup.Items.Count);

        // Roulette3は空のまま
        sut.SelectedGroup = sut.GroupList[2];
        Assert.Equal(0, sut.ItemCount);
        Assert.Empty(sut.SelectedGroup.Items);

        // Roulette1に戻ると、正しくアイテムが復元される
        sut.SelectedGroup = sut.GroupList[0];
        Assert.Equal(2, sut.ItemCount);
        Assert.Equal("Roulette1のアイテムA,1\nRoulette1のアイテムB,1", sut.ItemsText);
        Assert.Equal("Roulette1のアイテムA", sut.SelectedGroup.Items[0].Name);
        Assert.Equal("Roulette1のアイテムB", sut.SelectedGroup.Items[1].Name);

        // Roulette2に戻ると、正しくアイテムが復元される
        sut.SelectedGroup = sut.GroupList[1];
        Assert.Equal(3, sut.ItemCount);
        Assert.Equal("Roulette2のアイテムX,1\nRoulette2のアイテムY,1\nRoulette2のアイテムZ,1", sut.ItemsText);
        Assert.Equal("Roulette2のアイテムX", sut.SelectedGroup.Items[0].Name);
        Assert.Equal("Roulette2のアイテムY", sut.SelectedGroup.Items[1].Name);
        Assert.Equal("Roulette2のアイテムZ", sut.SelectedGroup.Items[2].Name);

        // 非同期保存の完了を待つ
        await Task.Delay(100);

        // SQLite にそれぞれのグループアイテムが保存されているはず（テスト簡略化）
        // 詳細な検証は FakeItemRepository を直接確認するか、統合テストで行う
    }
}
