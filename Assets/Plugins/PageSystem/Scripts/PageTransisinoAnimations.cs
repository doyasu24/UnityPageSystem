using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PageSystem
{
    /// <summary>
    /// Factory class providing built-in page transition animations.
    /// </summary>
    public static class PageTransitionAnimations
    {
        /// <summary>
        /// Gets a no-operation animation that completes instantly.
        /// </summary>
        public static IPageTransitionAnimation Nop => new NopPageTransitionAnimation();

        /// <summary>
        /// Creates a wait animation that delays for the specified duration without visual changes.
        /// </summary>
        /// <param name="duration">The duration to wait.</param>
        /// <returns>A wait animation instance.</returns>
        public static IPageTransitionAnimation Wait(TimeSpan duration) => new WaitPageTransitionAnimation(duration);

        /// <summary>
        /// Creates a linear slide animation between two anchor positions.
        /// </summary>
        /// <param name="fromAnchor">Starting anchor position (-1 to 1 range, relative to parent size).</param>
        /// <param name="toAnchor">Ending anchor position (-1 to 1 range, relative to parent size).</param>
        /// <param name="duration">The animation duration.</param>
        /// <returns>A linear slide animation instance.</returns>
        public static IPageTransitionAnimation Linear(Vector2 fromAnchor, Vector2 toAnchor, TimeSpan duration) =>
            new LinearPageTransitionAnimation(fromAnchor, toAnchor, duration);

        /// <summary>
        /// Creates a slide animation from off-screen left to center.
        /// </summary>
        /// <param name="duration">The animation duration.</param>
        /// <returns>A linear slide animation instance.</returns>
        public static IPageTransitionAnimation LinearFromLeftToCenter(TimeSpan duration) =>
            new LinearPageTransitionAnimation(new Vector2(-1, 0), Vector2.zero, duration);

        /// <summary>
        /// Creates a slide animation from center to off-screen left.
        /// </summary>
        /// <param name="duration">The animation duration.</param>
        /// <returns>A linear slide animation instance.</returns>
        public static IPageTransitionAnimation LinearFromCenterToLeft(TimeSpan duration) =>
            new LinearPageTransitionAnimation(Vector2.zero, new Vector2(-1, 0), duration);

        /// <summary>
        /// Creates a fade animation between two alpha values.
        /// </summary>
        /// <param name="from">Starting alpha value (0 to 1).</param>
        /// <param name="to">Ending alpha value (0 to 1).</param>
        /// <param name="duration">The animation duration.</param>
        /// <returns>An alpha fade animation instance.</returns>
        public static IPageTransitionAnimation Alpha(float from, float to, TimeSpan duration) =>
            new AlphaPageTransitionAnimation(from, to, duration);

        /// <summary>
        /// Creates a fade-in animation from transparent to opaque (0 to 1).
        /// </summary>
        /// <param name="duration">The animation duration.</param>
        /// <returns>An alpha fade-in animation instance.</returns>
        public static IPageTransitionAnimation AlphaIn(TimeSpan duration) =>
            new AlphaPageTransitionAnimation(0f, 1f, duration);

        /// <summary>
        /// Creates a fade-out animation from opaque to transparent (1 to 0).
        /// </summary>
        /// <param name="duration">The animation duration.</param>
        /// <returns>An alpha fade-out animation instance.</returns>
        public static IPageTransitionAnimation AlphaOut(TimeSpan duration) =>
            new AlphaPageTransitionAnimation(1f, 0f, duration);
    }

    /// <summary>
    /// A no-operation animation that completes instantly without any visual changes.
    /// </summary>
    public class NopPageTransitionAnimation : IPageTransitionAnimation
    {
        /// <inheritdoc />
        public async UniTask PlayAsync(RectTransform rectTransform, CancellationToken cancellationToken)
        {
            await UniTask.CompletedTask;
        }
    }

    /// <summary>
    /// An animation that waits for a specified duration without any visual changes.
    /// Useful for synchronizing with other concurrent animations.
    /// </summary>
    public class WaitPageTransitionAnimation : IPageTransitionAnimation
    {
        private readonly TimeSpan _duration;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitPageTransitionAnimation"/> class.
        /// </summary>
        /// <param name="duration">The duration to wait.</param>
        public WaitPageTransitionAnimation(TimeSpan duration)
        {
            _duration = duration;
        }

        /// <inheritdoc />
        public async UniTask PlayAsync(RectTransform rectTransform, CancellationToken cancellationToken)
        {
            await UniTask.Delay(_duration, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// An animation that fades the page by interpolating the CanvasGroup alpha value.
    /// </summary>
    public class AlphaPageTransitionAnimation : IPageTransitionAnimation
    {
        private readonly float _from;
        private readonly float _to;
        private readonly TimeSpan _duration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaPageTransitionAnimation"/> class.
        /// </summary>
        /// <param name="from">Starting alpha value (0 to 1).</param>
        /// <param name="to">Ending alpha value (0 to 1).</param>
        /// <param name="duration">The animation duration.</param>
        public AlphaPageTransitionAnimation(float from, float to, TimeSpan duration)
        {
            _from = from;
            _to = to;
            _duration = duration;
        }

        /// <inheritdoc />
        public async UniTask PlayAsync(RectTransform rectTransform, CancellationToken cancellationToken)
        {
            if (!rectTransform.gameObject.TryGetComponent<CanvasGroup>(out var canvasGroup))
                canvasGroup = rectTransform.gameObject.AddComponent<CanvasGroup>();

            var time = 0f;
            while (time < _duration.TotalSeconds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                time += Time.deltaTime;
                var t = Mathf.Clamp01(time / (float)_duration.TotalSeconds);
                canvasGroup.alpha = Mathf.Lerp(_from, _to, t);
                await UniTask.Yield(cancellationToken);
            }

            canvasGroup.alpha = _to;
        }
    }

    /// <summary>
    /// An animation that slides the page linearly between two anchor positions.
    /// </summary>
    /// <remarks>
    /// Anchor values are relative to the parent size:
    /// <list type="bullet">
    /// <item><description>(-1, 0): Off-screen left</description></item>
    /// <item><description>(1, 0): Off-screen right</description></item>
    /// <item><description>(0, -1): Off-screen bottom</description></item>
    /// <item><description>(0, 1): Off-screen top</description></item>
    /// <item><description>(0, 0): Center (on-screen)</description></item>
    /// </list>
    /// </remarks>
    public class LinearPageTransitionAnimation : IPageTransitionAnimation
    {
        private readonly Vector2 _fromAnchor;
        private readonly Vector2 _toAnchor;
        private readonly TimeSpan _duration;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearPageTransitionAnimation"/> class.
        /// </summary>
        /// <param name="fromAnchor">Starting anchor position (-1 to 1 range).</param>
        /// <param name="toAnchor">Ending anchor position (-1 to 1 range).</param>
        /// <param name="duration">The animation duration.</param>
        public LinearPageTransitionAnimation(Vector2 fromAnchor, Vector2 toAnchor, TimeSpan duration)
        {
            _fromAnchor = fromAnchor;
            _toAnchor = toAnchor;
            _duration = duration;
        }

        /// <inheritdoc />
        public async UniTask PlayAsync(RectTransform rectTransform, CancellationToken cancellationToken)
        {
            var parentSize = (rectTransform.parent as RectTransform)?.rect.size ?? Vector2.zero;
            var fromPosition = new Vector2(parentSize.x * _fromAnchor.x, parentSize.y * _fromAnchor.y);
            var toPosition = new Vector2(parentSize.x * _toAnchor.x, parentSize.y * _toAnchor.y);

            var time = 0f;
            while (time < _duration.TotalSeconds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                time += Time.deltaTime;
                var t = Mathf.Clamp01(time / (float)_duration.TotalSeconds);
                rectTransform.anchoredPosition = Vector2.Lerp(fromPosition, toPosition, t);
                await UniTask.Yield(cancellationToken);
            }

            rectTransform.anchoredPosition = toPosition;
        }
    }
}
