namespace PageSystem
{
    /// <summary>
    /// Provides global configuration for page transition animations.
    /// Set the animation properties at startup to define transition behavior.
    /// </summary>
    /// <remarks>
    /// Configure animations in your <c>LifetimeScope.Configure</c> method before any page transitions occur.
    /// </remarks>
    /// <example>
    /// <code>
    /// var duration = TimeSpan.FromSeconds(0.3f);
    /// PageTransitionAnimationProvider.PagePushEnter = PageTransitionAnimations.AlphaIn(duration);
    /// PageTransitionAnimationProvider.PagePushExit = PageTransitionAnimations.Wait(duration);
    /// PageTransitionAnimationProvider.PagePopEnter = PageTransitionAnimations.AlphaIn(duration);
    /// PageTransitionAnimationProvider.PagePopExit = PageTransitionAnimations.Wait(duration);
    /// </code>
    /// </example>
    public static class PageTransitionAnimationProvider
    {
        /// <summary>
        /// Gets or sets the animation played when a new page enters during a push operation.
        /// Default is <see cref="PageTransitionAnimations.Nop"/>.
        /// </summary>
        public static IPageTransitionAnimation PagePushEnter { get; set; } = PageTransitionAnimations.Nop;

        /// <summary>
        /// Gets or sets the animation played when the current page exits during a push operation.
        /// Default is <see cref="PageTransitionAnimations.Nop"/>.
        /// </summary>
        public static IPageTransitionAnimation PagePushExit { get; set; } = PageTransitionAnimations.Nop;

        /// <summary>
        /// Gets or sets the animation played when the previous page re-enters during a pop operation.
        /// Default is <see cref="PageTransitionAnimations.Nop"/>.
        /// </summary>
        public static IPageTransitionAnimation PagePopEnter { get; set; } = PageTransitionAnimations.Nop;

        /// <summary>
        /// Gets or sets the animation played when the current page exits during a pop operation.
        /// Default is <see cref="PageTransitionAnimations.Nop"/>.
        /// </summary>
        public static IPageTransitionAnimation PagePopExit { get; set; } = PageTransitionAnimations.Nop;

        /// <summary>
        /// Gets the appropriate animation for the specified transition type.
        /// </summary>
        /// <param name="push">True for push operations, false for pop operations.</param>
        /// <param name="enter">True for entering page, false for exiting page.</param>
        /// <returns>The configured animation for the specified transition.</returns>
        public static IPageTransitionAnimation Get(bool push, bool enter)
        {
            if (push)
                return enter ? PagePushEnter : PagePushExit;
            return enter ? PagePopEnter : PagePopExit;
        }
    }
}
