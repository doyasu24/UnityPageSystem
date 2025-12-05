using VContainer;
using VContainer.Unity;

namespace PageSystem
{
    /// <summary>
    /// Provides extension methods for registering page system components with VContainer.
    /// </summary>
    public static class PageVContainerExtensions
    {
        /// <summary>
        /// Registers the page system components with the container.
        /// </summary>
        /// <remarks>
        /// This method registers:
        /// <list type="bullet">
        /// <item><description><see cref="PagePublisher"/> as a singleton</description></item>
        /// <item><description><see cref="PageEntryPoint"/> as an entry point</description></item>
        /// </list>
        /// Call this in your <c>LifetimeScope.Configure</c> method along with registering your <see cref="PageContainer"/>.
        /// </remarks>
        /// <param name="builder">The container builder.</param>
        /// <example>
        /// <code>
        /// protected override void Configure(IContainerBuilder builder)
        /// {
        ///     builder.RegisterComponentInHierarchy&lt;PageContainer&gt;();
        ///     builder.RegisterPage();
        /// }
        /// </code>
        /// </example>
        public static void RegisterPage(this IContainerBuilder builder)
        {
            builder.Register<PagePublisher>(Lifetime.Singleton);
            builder.RegisterEntryPoint<PageEntryPoint>();
        }
    }
}
