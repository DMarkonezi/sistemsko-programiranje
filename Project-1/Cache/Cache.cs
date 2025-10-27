using multithreaded_web_server.Utils;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace multithreaded_web_server.Cache
{
    public class CacheItem
    {
        public string Content { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public CacheItem(string content)
        {
            Content = content;
            LastAccessedAt = DateTime.Now;
        }
    }

    public class CacheManager
    {
        public readonly ConcurrentDictionary<string, CacheItem> cache;
        private readonly int maxItemsInCache;
        private readonly TimeSpan exp;
        private readonly Timer cleanupTimer;

        public CacheManager(int maxItems = 100, int expMinutes = 30)
        {
            cache = new ConcurrentDictionary<string, CacheItem>();
            maxItemsInCache = maxItems;
            exp = TimeSpan.FromMinutes(expMinutes);
            cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        private void CleanupExpiredItems(object state)
        {
            var timeNow = DateTime.UtcNow;
            var expiredCacheItems = cache
                .Where(x => timeNow - x.Value.LastAccessedAt > exp)
                .Select(x => x.Key)
                .ToList();

            foreach (var item in expiredCacheItems)
            {
                cache.TryRemove(item, out _);
            }

            if (expiredCacheItems.Count > 0)
            {
                Logger.Log($"[CACHE] Cleaned up {expiredCacheItems.Count} expired items");
            }
        }
        
        // Dodavanje u cache
        public void Add(string filename, string content)
        {
            if (cache.Count >= maxItemsInCache)
            {
                RemoveLeastRecentlyUsedCacheItem();
            }

            cache[filename] = new CacheItem(content);
        }

        // Uzimanje iz kesa
        public bool TryGet(string filename, out string content) 
        { 
            if (cache.TryGetValue(filename, out CacheItem item))
            {
                // Proverava da li je vreme isteklo za konkretan 

                if (DateTime.UtcNow - item.LastAccessedAt > exp)
                {
                    cache.TryRemove(filename, out _);
                    content = null;
                    return false;
                }

                // Azurira mu vreme i onda izbacuje zeljeni contentm, odnosno path
                item.LastAccessedAt = DateTime.UtcNow;
                content = item.Content;
                return true;
            }

            content = null;
            return false;
        }

        public void RemoveLeastRecentlyUsedCacheItem()
        {
            if (cache.IsEmpty) return;

            var oldestItemInCache = cache.OrderBy(x => x.Value.LastAccessedAt).FirstOrDefault();

            if (!string.IsNullOrEmpty(oldestItemInCache.Key))
            {
                cache.TryRemove(oldestItemInCache.Key, out _);
                Logger.Log($"[CACHE] Evicted LRU item: {oldestItemInCache.Key}");
            }
        }

        public void Clear()
        {
            cache.Clear();
        }

        public void Dispose()
        {
            cleanupTimer.Dispose();
        }
    }
}
