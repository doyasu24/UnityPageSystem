using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PageSystem
{
    /// <summary>
    /// Defines a page transition animation that can be played during page navigation.
    /// </summary>
    /// <remarks>
    /// Implement this interface to create custom transition animations.
    /// Built-in implementations are available via <see cref="PageTransitionAnimations"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class ScaleAnimation : IPageTransitionAnimation
    /// {
    ///     private readonly float _from, _to;
    ///     private readonly TimeSpan _duration;
    ///
    ///     public ScaleAnimation(float from, float to, TimeSpan duration)
    ///     {
    ///         _from = from; _to = to; _duration = duration;
    ///     }
    ///
    ///     public async UniTask PlayAsync(RectTransform rectTransform, CancellationToken cancellationToken)
    ///     {
    ///         var time = 0f;
    ///         while (time &lt; _duration.TotalSeconds)
    ///         {
    ///             cancellationToken.ThrowIfCancellationRequested();
    ///             time += Time.deltaTime;
    ///             var t = Mathf.Clamp01(time / (float)_duration.TotalSeconds);
    ///             var scale = Mathf.Lerp(_from, _to, t);
    ///             rectTransform.localScale = new Vector3(scale, scale, 1f);
    ///             await UniTask.Yield(cancellationToken);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IPageTransitionAnimation
    {
        /// <summary>
        /// Plays the transition animation on the specified RectTransform.
        /// </summary>
        /// <param name="rectTransform">The RectTransform to animate.</param>
        /// <param name="cancellationToken">Token to cancel the animation.</param>
        /// <returns>A task representing the animation playback.</returns>
        UniTask PlayAsync(RectTransform rectTransform, CancellationToken cancellationToken);
    }
}
