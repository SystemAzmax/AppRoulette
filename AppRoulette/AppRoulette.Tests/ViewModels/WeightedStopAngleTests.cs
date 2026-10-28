using AppRoulette.Models;
using AppRoulette.Tests.Fakes;
using AppRoulette.ViewModels;

namespace AppRoulette.Tests.ViewModels;

/// <summary>
/// 重み付きルーレットの停止位置（<see cref="MainViewModel.CalcTargetAngle"/>）のテスト。
/// 重み付き抽選で選ばれたアイテムと、ルーレットが止まったときに
/// インジケーターが指す扇形が一致することを検証します。
/// </summary>
public class WeightedStopAngleTests
{
    /// <summary>扇形の中央とみなす位置の許容誤差（扇形の幅に対する割合）。</summary>
    private const double CENTER_TOLERANCE = 0.001;

    // ---------------------------------------------------------------
    // ヘルパー
    // ---------------------------------------------------------------

    /// <summary>
    /// 指定した Weight を持つアイテム一覧を作成します。
    /// </summary>
    /// <param name="weights">各アイテムの Weight。</param>
    /// <returns>アイテム一覧。</returns>
    private static List<RouletteItem> CreateItems(params int[] weights) =>
        weights
            .Select((weight, i) => new RouletteItem($"アイテム{i + 1}") { Weight = weight })
            .ToList();

    /// <summary>
    /// 指定した回転角度のときに、インジケーターが指している扇形と、
    /// その扇形の中での位置を返します。
    /// 扇形の配置は RouletteRenderer.Draw と同じで、扇形 i は
    /// 「回転角度 - π/2 + 2π × (i より前の Weight の合計) / Weight の総和」から始まり、
    /// インジケーターは 3 時方向（角度 0）にあります。
    /// </summary>
    /// <param name="items">盤面に描画するアイテム一覧。</param>
    /// <param name="rotationAngle">盤面の回転角度（ラジアン）。</param>
    /// <returns>扇形のインデックスと、扇形の中での位置（開始端 0.0 ～ 終了端 1.0）。</returns>
    private static (int Index, double PositionInSector) GetIndicatorPosition(
        IReadOnlyList<RouletteItem> items,
        float rotationAngle)
    {
        var fullTurn = 2d * Math.PI;
        var totalWeight = items.Sum(item => item.Weight);

        // 盤面の開始端からインジケーター（角度 0）までの角度を 0 ～ 2π に正規化する
        var offset = (0d - (rotationAngle - Math.PI / 2d)) % fullTurn;
        if (offset < 0d)
        {
            offset += fullTurn;
        }

        var sectorStart = 0d;
        for (var i = 0; i < items.Count; i++)
        {
            var sweep = fullTurn * items[i].Weight / totalWeight;
            if (offset < sectorStart + sweep || i == items.Count - 1)
            {
                return (i, (offset - sectorStart) / sweep);
            }

            sectorStart += sweep;
        }

        throw new ArgumentException("アイテムがありません。", nameof(items));
    }

    /// <summary>
    /// MainWindow.StartSpinAnimation と同じ手順で、目標角度に向けたスピンを
    /// 開始し、アニメーションが完了するまで進めます。
    /// </summary>
    /// <param name="spinAnimation">アニメーション状態。</param>
    /// <param name="targetAngle">目標回転角度（ラジアン）。</param>
    private static void SpinToCompletion(
        SpinAnimationViewModel spinAnimation,
        float targetAngle)
    {
        spinAnimation.SpinStartAngle = spinAnimation.RotationAngle;
        spinAnimation.SpinElapsedSec = 0f;

        var rawDelta = targetAngle - spinAnimation.SpinStartAngle;
        while (rawDelta < SpinAnimationViewModel.MIN_SPIN_RADIANS)
        {
            rawDelta += MathF.PI * 2f;
        }

        while (rawDelta > SpinAnimationViewModel.MAX_SPIN_RADIANS)
        {
            rawDelta -= MathF.PI * 2f;
        }

        spinAnimation.SpinTotalRadians = rawDelta;

        while (!spinAnimation.IsAnimationComplete)
        {
            spinAnimation.AdvanceAnimation(SpinAnimationViewModel.TIMER_INTERVAL_MS);
        }
    }

    /// <summary>
    /// 固定値を返す乱数サービスで、グループ1に指定アイテムを持つ ViewModel を初期化します。
    /// </summary>
    /// <param name="randomValue">乱数サービスが返す固定値。</param>
    /// <param name="weights">グループ1の各アイテムの Weight。</param>
    /// <returns>初期化済みの ViewModel。</returns>
    private static async Task<MainViewModel> CreateInitializedSutAsync(
        int randomValue,
        params int[] weights)
    {
        var fakeRepo = new FakeItemRepository();
        fakeRepo.InitializeWithItems(weights
            .Select((weight, i) => new Item($"アイテム{i + 1}", weight, groupId: 1))
            .ToList());

        var sut = new MainViewModel(
            new FakeRandomService(randomValue),
            fakeRepo,
            new FakeGroupRepository(),
            new FakeAppSettingsRepository());
        await sut.InitializeCommand.ExecuteAsync(null);
        return sut;
    }

    // ---------------------------------------------------------------
    // CalcTargetAngle
    // ---------------------------------------------------------------

    /// <summary>
    /// Weight の組み合わせごとに、どのアイテムを選んでも、目標角度まで回すと
    /// そのアイテムの扇形の中央にインジケーターが来ることを検証します。
    /// </summary>
    /// <param name="weights">各アイテムの Weight。</param>
    [Theory]
    [InlineData(new[] { 5 })]
    [InlineData(new[] { 1, 1, 1 })]
    [InlineData(new[] { 1, 2, 3 })]
    [InlineData(new[] { 5, 1 })]
    [InlineData(new[] { 3, 1, 5, 2, 4 })]
    [InlineData(new[] { 1, 5, 1, 5, 1, 5 })]
    public void CalcTargetAngle_Weightの組み合わせ_選ばれた扇形の中央がインジケーターに来る(
        int[] weights)
    {
        // Arrange
        var items = CreateItems(weights);

        for (var selectedIndex = 0; selectedIndex < items.Count; selectedIndex++)
        {
            // Act
            var targetAngle = MainViewModel.CalcTargetAngle(selectedIndex, items);
            var (index, positionInSector) = GetIndicatorPosition(items, targetAngle);

            // Assert
            Assert.Equal(selectedIndex, index);
            Assert.Equal(0.5, positionInSector, CENTER_TOLERANCE);
        }
    }

    /// <summary>
    /// 最大件数（30 件）で Weight が混在する場合も、選ばれた扇形の中央に
    /// インジケーターが来ることを検証します。
    /// </summary>
    [Fact]
    public void CalcTargetAngle_最大件数でWeightが混在する場合_選ばれた扇形の中央がインジケーターに来る()
    {
        // Arrange
        var weights = Enumerable.Range(0, RouletteGroup.MAX_ITEM_COUNT)
            .Select(i => i % 5 + 1)
            .ToArray();
        var items = CreateItems(weights);

        for (var selectedIndex = 0; selectedIndex < items.Count; selectedIndex++)
        {
            // Act
            var targetAngle = MainViewModel.CalcTargetAngle(selectedIndex, items);
            var (index, positionInSector) = GetIndicatorPosition(items, targetAngle);

            // Assert
            Assert.Equal(selectedIndex, index);
            Assert.Equal(0.5, positionInSector, CENTER_TOLERANCE);
        }
    }

    /// <summary>
    /// アイテムが無い場合や、インデックスが範囲外の場合は 0 を返すことを検証します。
    /// </summary>
    /// <param name="selectedIndex">選択インデックス。</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void CalcTargetAngle_範囲外のインデックス_0を返す(int selectedIndex)
    {
        // Arrange
        var items = CreateItems(1, 2, 3);

        // Act
        var targetAngle = MainViewModel.CalcTargetAngle(selectedIndex, items);
        var nullItemsAngle = MainViewModel.CalcTargetAngle(0, null);

        // Assert
        Assert.Equal(0f, targetAngle);
        Assert.Equal(0f, nullItemsAngle);
    }

    // ---------------------------------------------------------------
    // 抽選結果との整合性
    // ---------------------------------------------------------------

    /// <summary>
    /// 重み付き抽選で選ばれたアイテムと、停止位置でインジケーターが指す扇形の
    /// アイテムが一致することを、Weight 上のすべての抽選値について検証します。
    /// </summary>
    [Fact]
    public async Task Spin_重み付きで選ばれたアイテム_停止位置の扇形のアイテムと一致する()
    {
        // Arrange
        var weights = new[] { 1, 3, 2, 4 };
        var totalWeight = weights.Sum();

        for (var randomValue = 0; randomValue < totalWeight; randomValue++)
        {
            var sut = await CreateInitializedSutAsync(randomValue, weights);

            // Act
            sut.SpinCommand.Execute(null);
            var wheelItems = sut.WheelItems;
            var selectedItem = wheelItems[sut.SelectedItemIndex];
            var targetAngle = MainViewModel.CalcTargetAngle(sut.SelectedItemIndex, wheelItems);
            var (index, positionInSector) = GetIndicatorPosition(wheelItems, targetAngle);

            // Assert
            Assert.Same(selectedItem, wheelItems[index]);
            Assert.Equal(0.5, positionInSector, CENTER_TOLERANCE);
        }
    }

    /// <summary>
    /// 無効なアイテムが盤面から外れている場合も、選ばれたアイテムと
    /// 停止位置の扇形のアイテムが一致することを検証します。
    /// </summary>
    [Fact]
    public async Task Spin_無効なアイテムがある場合_停止位置の扇形のアイテムと一致する()
    {
        // Arrange
        var weights = new[] { 2, 5, 1, 3 };
        const int disabledIndex = 1;
        var enabledTotalWeight = weights.Sum() - weights[disabledIndex];

        for (var randomValue = 0; randomValue < enabledTotalWeight; randomValue++)
        {
            var sut = await CreateInitializedSutAsync(randomValue, weights);
            sut.SelectedGroup!.Items[disabledIndex].IsEnabled = false;

            // Act
            sut.SpinCommand.Execute(null);
            var wheelItems = sut.WheelItems;
            var selectedItem = wheelItems[sut.SelectedItemIndex];
            var targetAngle = MainViewModel.CalcTargetAngle(sut.SelectedItemIndex, wheelItems);
            var (index, positionInSector) = GetIndicatorPosition(wheelItems, targetAngle);

            // Assert
            Assert.True(selectedItem.IsEnabled);
            Assert.Same(selectedItem, wheelItems[index]);
            Assert.Equal(0.5, positionInSector, CENTER_TOLERANCE);
        }
    }

    // ---------------------------------------------------------------
    // アニメーションとの整合性
    // ---------------------------------------------------------------

    /// <summary>
    /// アニメーションで回転角度が積み重なっていっても、毎回のスピンが
    /// 選ばれた扇形の中央で止まることを検証します。
    /// </summary>
    [Fact]
    public void SpinAnimation_連続してスピンした場合_毎回選ばれた扇形の中央で止まる()
    {
        // Arrange
        var items = CreateItems(2, 5, 1, 3, 4);
        var spinAnimation = new SpinAnimationViewModel();

        for (var spin = 0; spin < 20; spin++)
        {
            var selectedIndex = spin * 3 % items.Count;
            var targetAngle = MainViewModel.CalcTargetAngle(selectedIndex, items);

            // Act
            SpinToCompletion(spinAnimation, targetAngle);
            var (index, positionInSector) =
                GetIndicatorPosition(items, spinAnimation.RotationAngle);

            // Assert
            Assert.Equal(selectedIndex, index);
            Assert.Equal(0.5, positionInSector, CENTER_TOLERANCE);
        }
    }
}
