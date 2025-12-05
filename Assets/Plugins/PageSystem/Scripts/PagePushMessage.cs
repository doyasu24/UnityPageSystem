namespace PageSystem
{
    /// <summary>
    /// Message sent through <see cref="PagePublisher"/> to request a page push operation.
    /// </summary>
    public class PagePushMessage
    {
        /// <summary>
        /// The Addressable resource key for the page to load.
        /// </summary>
        public readonly string ResourceKey;

        /// <summary>
        /// Whether to play transition animations.
        /// </summary>
        public readonly bool PlayAnimation;

        /// <summary>
        /// Whether to keep the current page in the stack.
        /// </summary>
        public readonly bool Stack;

        private PagePushMessage(string resourceKey, bool playAnimation, bool stack)
        {
            ResourceKey = resourceKey;
            PlayAnimation = playAnimation;
            Stack = stack;
        }

        /// <summary>
        /// Creates a new push message with the specified resource key.
        /// </summary>
        /// <param name="resourceKey">The Addressable resource key for the page prefab.</param>
        /// <param name="playAnimation">Whether to play transition animations.</param>
        /// <param name="isStack">Whether to keep the current page in the stack.</param>
        /// <returns>A new push message instance.</returns>
        public static PagePushMessage Create(string resourceKey, bool playAnimation, bool isStack)
        {
            return new PagePushMessage(resourceKey, playAnimation, isStack);
        }

        /// <summary>
        /// Creates a new push message for the specified page type.
        /// </summary>
        /// <typeparam name="TPage">The type of page to push.</typeparam>
        /// <param name="playAnimation">Whether to play transition animations.</param>
        /// <param name="isStack">Whether to keep the current page in the stack.</param>
        /// <returns>A new push message instance.</returns>
        public static PagePushMessage Create<TPage>(bool playAnimation, bool isStack)
            where TPage : Page
        {
            var resourceKey = ResourceKeyAttribute.FindOrThrow(typeof(TPage));
            return new PagePushMessage(resourceKey, playAnimation, isStack);
        }
    }
}
