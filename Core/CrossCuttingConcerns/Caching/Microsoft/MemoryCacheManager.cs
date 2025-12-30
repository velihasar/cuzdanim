using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Utilities.IoC;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Text;
using static ServiceStack.Diagnostics;

namespace Core.CrossCuttingConcerns.Caching.Microsoft
{
    /// <summary>
    /// Microsoft MemoryCacheManager
    /// </summary>
    public class MemoryCacheManager : ICacheManager
    {
        private readonly IMemoryCache _cache;
        private readonly HashSet<string> _cacheKeys;

        public MemoryCacheManager()
            : this(ServiceTool.ServiceProvider.GetService<IMemoryCache>())
        {
        }

        public MemoryCacheManager(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _cacheKeys = new HashSet<string>();
        }

        public void Add(string key, object data, int duration)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            _cache.Set(key, data, TimeSpan.FromMinutes(duration));
            _cacheKeys.Add(key);
        }

        public void Add(string key, object data)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            _cache.Set(key, data);
            _cacheKeys.Add(key);
        }

        public void Add(string key, dynamic data, int duration, Type type)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            var json = JsonSerializer.SerializeToString(data.Result, type);
            Add(key, json, duration);
        }

        public void Add(string key, dynamic data, Type type)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            var json = JsonSerializer.SerializeToString(data.Result, type);
            Add(key, json);
        }

        public T Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default(T);

            return _cache.Get<T>(key);
        }

        public object Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            return _cache.Get(key);
        }

        public object Get(string key, Type type)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            var json = Get<string>(key);
            if (json == null)
                return null;

            var result = JsonSerializer.DeserializeFromString(json, type);

            return typeof(Task)
                .GetMethod(nameof(Task.FromResult))
                .MakeGenericMethod(type)
                .Invoke(this, new object[] { result });
        }

        public bool IsAdd(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return _cache.TryGetValue(key, out _);
        }

        public void Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            _cache.Remove(key);
            _cacheKeys.Remove(key);
        }

        public void RemoveByPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return;

            try
            {
                var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var keysToRemove = _cacheKeys.Where(key => regex.IsMatch(key)).ToList();

                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                    _cacheKeys.Remove(key);
                }
            }
            catch (Exception ex)
            {
                // Log the exception but don't throw to prevent application crash
                // In a real application, you might want to use a proper logging framework
                System.Diagnostics.Debug.WriteLine($"Error in RemoveByPattern: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all cache keys for debugging purposes
        /// </summary>
        /// <returns>List of all cache keys</returns>
        public IEnumerable<string> GetAllKeys()
        {
            return _cacheKeys.ToList();
        }

        /// <summary>
        /// Clears all cache entries
        /// </summary>
        public void Clear()
        {
            foreach (var key in _cacheKeys.ToList())
            {
                _cache.Remove(key);
            }
            _cacheKeys.Clear();
        }
    }
}