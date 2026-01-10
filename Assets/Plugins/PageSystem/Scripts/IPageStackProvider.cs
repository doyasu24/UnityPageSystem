using System.Collections.Generic;

namespace PageSystem
{
    /// <summary>
    /// Provides access to the page navigation stack.
    /// </summary>
    public interface IPageStackProvider
    {
        /// <summary>
        /// Gets the current (topmost) page in the navigation stack.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown when no pages are loaded.</exception>
        PageStackInfo CurrentPage { get; }

        /// <summary>
        /// Gets the complete page stack in order (oldest first, newest last).
        /// The last element is the current page.
        /// </summary>
        IReadOnlyList<PageStackInfo> Stack { get; }

        /// <summary>
        /// Gets the number of pages currently in the stack.
        /// </summary>
        int PageCount { get; }
    }
}
