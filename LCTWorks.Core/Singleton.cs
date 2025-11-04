using System.Reflection;

namespace LCTWorks.Core
{
    public abstract class Singleton<T> where T : class
    {
        private static readonly Lazy<T> _instance =
            new(CreateInstance, LazyThreadSafetyMode.ExecutionAndPublication);

        protected Singleton()
        {
            if (_instance.IsValueCreated && !ReferenceEquals(this, _instance.Value))
            {
                throw new InvalidOperationException($"{typeof(T).FullName} is a singleton and cannot be instantiated more than once.");
            }
        }

        public static T Instance => _instance.Value;

        public static bool IsValueCreated => _instance.IsValueCreated;

        public static bool TryGetInstance(out T? instance)
        {
            if (_instance.IsValueCreated)
            {
                instance = _instance.Value;
                return true;
            }

            instance = null;
            return false;
        }

        private static T CreateInstance()
        {
            var type = typeof(T);
            var ctor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);

            return ctor is null
                ? throw new InvalidOperationException($"{type.FullName} must have a parameterless constructor (private or protected) to be used with Singleton<T>.")
                : (T)ctor.Invoke(null);
        }
    }
}