namespace PageSystem
{
    /// <summary>
    /// Represents a single entry in the page navigation history.
    /// </summary>
    public readonly struct PageHistoryEntry
    {
        public readonly string ResourceKey;
        public readonly string PageId;
        public readonly Page Page;

        public PageHistoryEntry(string resourceKey, string pageId, Page page)
        {
            ResourceKey = resourceKey;
            PageId = pageId;
            Page = page;
        }
    }
}
