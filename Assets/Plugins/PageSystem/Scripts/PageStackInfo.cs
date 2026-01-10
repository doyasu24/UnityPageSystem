namespace PageSystem
{
    /// <summary>
    /// Represents information about a page in the navigation stack.
    /// This is a read-only view of a page's state for external consumers.
    /// </summary>
    public readonly struct PageStackInfo
    {
        /// <summary>
        /// The Addressable resource key for the page prefab.
        /// </summary>
        public readonly string ResourceKey;

        /// <summary>
        /// The unique identifier for this page instance.
        /// </summary>
        public readonly string PageId;

        /// <summary>
        /// Indicates whether this page will be retained in the stack when a new page is pushed.
        /// If true, the page remains in memory and can be returned to via Pop.
        /// If false, the page will be destroyed when a new page is pushed.
        /// </summary>
        public readonly bool Stacked;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageStackInfo"/> struct.
        /// </summary>
        /// <param name="resourceKey">The Addressable resource key.</param>
        /// <param name="pageId">The unique page identifier.</param>
        /// <param name="stacked">Whether the page should be retained when a new page is pushed.</param>
        public PageStackInfo(string resourceKey, string pageId, bool stacked)
        {
            ResourceKey = resourceKey;
            PageId = pageId;
            Stacked = stacked;
        }
    }
}
