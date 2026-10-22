using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace AppRoulette.ViewModels;

/// <summary>
/// ルーレット回転のアニメーション状態と計算を管理する ViewModel。
/// アニメーションの進行状況、回転角度、easing 計算などを専門に扱います。
/// </summary>
public class SpinAnimationViewModel : ObservableObject
{
    /// <summary>1フレームのインターバル（ミリ秒）。60fps。</summary>
    public const int TIMER_INTERVAL_MS = 16;

    /// <summary>アニメーションの総回転量（ラジアン）の最小値。</summary>
    public const float MIN_SPIN_RADIANS = MathF.PI * 8f;   // 4周

    /// <summary>アニメーションの総回転量（ラジアン）の最大値。</summary>
    public const float MAX_SPIN_RADIANS = MathF.PI * 14f;  // 7周

    /// <summary>アニメーション総時間（秒）。</summary>
    public const float SPIN_DURATION_SEC = 3.5f;

    /// <summary>現在の回転角度（ラジアン）。</summary>
    private float _rotationAngle;

    /// <summary>アニメーション開始時点の回転角度（ラジアン）。</summary>
    private float _spinStartAngle;

    /// <summary>アニメーションの総回転量（ラジアン）。</summary>
    private float _spinTotalRadians;

    /// <summary>アニメーション経過時間（秒）。</summary>
    private float _spinElapsedSec;

    /// <summary>
    /// 現在の回転角度（ラジアン）を取得または設定します。
    /// </summary>
    public float RotationAngle
    {
        get => _rotationAngle;
        set => SetProperty(ref _rotationAngle, value);
    }

    /// <summary>
    /// アニメーション開始時点の回転角度（ラジアン）を取得または設定します。
    /// </summary>
    public float SpinStartAngle
    {
        get => _spinStartAngle;
        set => SetProperty(ref _spinStartAngle, value);
    }

    /// <summary>
    /// アニメーションの総回転量（ラジアン）を取得または設定します。
    /// </summary>
    public float SpinTotalRadians
    {
        get => _spinTotalRadians;
        set => SetProperty(ref _spinTotalRadians, value);
    }

    /// <summary>
    /// アニメーション経過時間（秒）を取得または設定します。
    /// </summary>
    public float SpinElapsedSec
    {
        get => _spinElapsedSec;
        set => SetProperty(ref _spinElapsedSec, value);
    }

    /// <summary>
    /// アニメーションの進行率（0.0 ～ 1.0）を計算して取得します。
    /// </summary>
    public float AnimationProgress
    {
        get => Math.Min(SpinElapsedSec / SPIN_DURATION_SEC, 1f);
    }

    /// <summary>
    /// アニメーションが完了したかを判定します。
    /// </summary>
    public bool IsAnimationComplete
    {
        get => AnimationProgress >= 1f;
    }

    /// <summary>
    /// easing 計算なしの現在の回転角度を単純に計算します（デバッグ用）。
    /// </summary>
    public float GetCurrentRotationWithoutEasing()
    {
        var progress = AnimationProgress;
        return SpinStartAngle + SpinTotalRadians * progress;
    }

    /// <summary>
    /// easing 計算付きの現在の回転角度を計算します。
    /// </summary>
    public float GetCurrentRotationWithEasing()
    {
        var progress = AnimationProgress;
        var eased = EaseOutCubic(progress);
        return SpinStartAngle + SpinTotalRadians * eased;
    }

    /// <summary>
    /// Cubic easing out 関数。
    /// アニメーション開始時は速く、終了に向かって減速します。
    /// </summary>
    /// <param name="t">進行率（0.0 ～ 1.0）。</param>
    /// <returns>easing 計算結果。</returns>
    public static float EaseOutCubic(float t)
    {
        // f(t) = 1 - (1 - t)^3
        var oneMinusT = 1f - t;
        return 1f - (oneMinusT * oneMinusT * oneMinusT);
    }

    /// <summary>
    /// アニメーション状態を初期化します。
    /// </summary>
    public void ResetAnimation()
    {
        RotationAngle = 0f;
        SpinStartAngle = 0f;
        SpinTotalRadians = 0f;
        SpinElapsedSec = 0f;
    }

    /// <summary>
    /// アニメーション経過時間を進めます。
    /// </summary>
    /// <param name="deltaMs">進める時間（ミリ秒）。</param>
    public void AdvanceAnimation(int deltaMs)
    {
        SpinElapsedSec += deltaMs / 1000f;
        RotationAngle = GetCurrentRotationWithEasing();
    }
}
