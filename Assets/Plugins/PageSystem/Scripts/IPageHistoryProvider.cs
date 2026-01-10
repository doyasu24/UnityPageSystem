using System.Collections.Generic;

namespace PageSystem
{
    /// <summary>
    /// Provides access to page navigation history.
    /// </summary>
    public interface IPageHistoryProvider
    {
        /// <summary>
        /// Gets the current (topmost) page in the navigation stack.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown when no pages are loaded.</exception>
        PageHistoryEntry CurrentPage { get; }

        /// <summary>
        /// Gets the complete page history in stack order (oldest first, newest last).
        /// The last element is the current page.
        /// </summary>
        IReadOnlyList<PageHistoryEntry> History { get; }
    }
}
