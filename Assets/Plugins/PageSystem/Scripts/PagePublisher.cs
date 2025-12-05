using System;
using Cysharp.Threading.Tasks;
using VContainer;

namespace PageSystem
{
    /// <summary>
    /// Provides methods to navigate between pages using push/pop operations.
    /// This is the main entry point for page navigation.
    /// </summary>
    /// <remarks>
    /// PagePublisher uses async channels to queue navigation requests,
    /// which are processed by <see cref="PageEntryPoint"/>.
    /// Register this class using <see cref="PageVContainerExtensions.RegisterPage"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class NavigationController
    /// {
    ///     private readonly PagePublisher _pagePublisher;
    ///
    ///     [Inject]
    ///     public NavigationController(PagePublisher pagePublisher)
    ///     {
    ///         _pagePublisher = pagePublisher;
    ///     }
    ///
    ///     public void GoToSettings() => _pagePublisher.Push&lt;SettingsPage&gt;();
    ///     public void GoBack() => _pagePublisher.Pop();
    /// }
    /// </code>
    /// </example>
    public class PagePublisher : IDisposable
    {
        private readonly Channel<PagePushMessage> _pushChannel;

        /// <summary>
        /// Gets the channel reader for push messages. Used internally by PageEntryPoint.
        /// </summary>
        public ChannelReader<PagePushMessage> PushChannel => _pushChannel.Reader;

        private readonly Channel<PagePopMessage> _popChannel;

        /// <summary>
        /// Gets the channel reader for pop messages. Used internally by PageEntryPoint.
        /// </summary>
        public ChannelReader<PagePopMessage> PopChannel => _popChannel.Reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="PagePublisher"/> class.
        /// </summary>
        [Inject]
        public PagePublisher()
        {
            _pushChannel = Channel.CreateSingleConsumerUnbounded<PagePushMessage>();
            _popChannel = Channel.CreateSingleConsumerUnbounded<PagePopMessage>();
        }

        /// <summary>
        /// Pushes a new page onto the navigation stack.
        /// </summary>
        /// <typeparam name="TPage">
        /// The type of page to push. Must inherit from <see cref="Page"/> and have
        /// a <see cref="ResourceKeyAttribute"/> specifying the Addressable key.
        /// </typeparam>
        /// <param name="playAnimation">
        /// If <c>true</c>, plays the configured transition animations.
        /// If <c>false</c>, the transition is instant.
        /// </param>
        /// <param name="isStack">
        /// If <c>true</c>, the current page is kept in memory and can be returned to via <see cref="Pop"/>.
        /// If <c>false</c>, the current page is destroyed after the transition.
        /// </param>
        /// <example>
        /// <code>
        /// // Push with animation, keeping current page in stack
        /// _pagePublisher.Push&lt;SettingsPage&gt;();
        ///
        /// // Push without animation (useful for initial page)
        /// _pagePublisher.Push&lt;MainPage&gt;(playAnimation: false);
        ///
        /// // Replace current page (don't keep in stack)
        /// _pagePublisher.Push&lt;GameOverPage&gt;(isStack: false);
        /// </code>
        /// </example>
        public void Push<TPage>(bool playAnimation = true, bool isStack = true) where TPage : Page
        {
            var message = PagePushMessage.Create<TPage>(playAnimation, isStack);
            _pushChannel.Writer.TryWrite(message);
        }

        /// <summary>
        /// Pushes a new page onto the navigation stack using the specified resource key.
        /// </summary>
        /// <param name="pageResourceKey">The Addressable resource key for the page prefab.</param>
        /// <param name="playAnimation">
        /// If <c>true</c>, plays the configured transition animations.
        /// If <c>false</c>, the transition is instant.
        /// </param>
        /// <param name="isStack">
        /// If <c>true</c>, the current page is kept in memory and can be returned to via <see cref="Pop"/>.
        /// If <c>false</c>, the current page is destroyed after the transition.
        /// </param>
        public void Push(string pageResourceKey, bool playAnimation = true, bool isStack = true)
        {
            var message = PagePushMessage.Create(pageResourceKey, playAnimation, isStack);
            _pushChannel.Writer.TryWrite(message);
        }

        /// <summary>
        /// Pops the current page from the navigation stack and returns to the previous page.
        /// </summary>
        /// <param name="playAnimation">
        /// If <c>true</c>, plays the configured transition animations.
        /// If <c>false</c>, the transition is instant.
        /// </param>
        /// <example>
        /// <code>
        /// // Pop with animation
        /// _pagePublisher.Pop();
        ///
        /// // Pop without animation
        /// _pagePublisher.Pop(playAnimation: false);
        /// </code>
        /// </example>
        public void Pop(bool playAnimation = true)
        {
            var message = PagePopMessage.Create(playAnimation);
            _popChannel.Writer.TryWrite(message);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="PagePublisher"/>.
        /// Completes the push and pop channels.
        /// </summary>
        public void Dispose()
        {
            _pushChannel.Writer.TryComplete();
            _popChannel.Writer.TryComplete();
        }
    }
}
