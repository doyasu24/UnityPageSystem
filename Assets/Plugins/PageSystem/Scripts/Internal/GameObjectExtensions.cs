using UnityEngine;

namespace PageSystem.Internal
{
    internal static class GameObjectExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject self) where T : Component =>
            self.TryGetComponent<T>(out var component) ? component : self.AddComponent<T>();
    }
}
