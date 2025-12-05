using System;

namespace PageSystem
{
    /// <summary>
    /// Specifies the Addressable asset key for a <see cref="Page"/> class.
    /// </summary>
    /// <remarks>
    /// Apply this attribute to your Page subclass to associate it with an Addressable prefab.
    /// The key must match the Addressable address configured in Unity's Addressables window.
    /// </remarks>
    /// <example>
    /// <code>
    /// [ResourceKey("MainPage")]
    /// public class MainPage : Page
    /// {
    ///     // Page implementation
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ResourceKeyAttribute : Attribute
    {
        /// <summary>
        /// Gets the Addressable resource key.
        /// </summary>
        public string ResourceKey { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceKeyAttribute"/> class.
        /// </summary>
        /// <param name="resourceKey">The Addressable asset key for the page prefab.</param>
        public ResourceKeyAttribute(string resourceKey)
        {
            ResourceKey = resourceKey;
        }

        /// <summary>
        /// Finds and returns the resource key for the specified type.
        /// </summary>
        /// <param name="type">The type to find the resource key for.</param>
        /// <returns>The resource key string.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the type does not have a <see cref="ResourceKeyAttribute"/> defined,
        /// or when the resource key is null or empty.
        /// </exception>
        public static string FindOrThrow(Type type)
        {
            var attr = GetCustomAttribute(type, typeof(ResourceKeyAttribute)) as ResourceKeyAttribute
                       ?? throw new InvalidOperationException(
                           $"The type {type.Name} does not have a ResourceKeyAttribute defined.");
            if (string.IsNullOrEmpty(attr.ResourceKey))
            {
                throw new InvalidOperationException(
                    $"The ResourceKeyAttribute for type {type.Name} has an empty or null ResourceKey.");
            }

            return attr.ResourceKey;
        }
    }
}
