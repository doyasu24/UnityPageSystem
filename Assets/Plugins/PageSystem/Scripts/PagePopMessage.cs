namespace PageSystem
{
    /// <summary>
    /// Message sent through <see cref="PagePublisher"/> to request a page pop operation.
    /// </summary>
    public class PagePopMessage
    {
        /// <summary>
        /// Whether to play transition animations.
        /// </summary>
        public readonly bool PlayAnimation;

        private PagePopMessage(bool playAnimation)
        {
            PlayAnimation = playAnimation;
        }

        /// <summary>
        /// Creates a new pop message.
        /// </summary>
        /// <param name="playAnimation">Whether to play transition animations.</param>
        /// <returns>A new pop message instance.</returns>
        public static PagePopMessage Create(bool playAnimation)
        {
            return new PagePopMessage(playAnimation);
        }
    }
}
