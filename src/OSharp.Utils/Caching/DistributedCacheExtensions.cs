// -----------------------------------------------------------------------
//  <copyright file="DistributedCacheExtensions.cs" company="OSharp开源团队">
//      Copyright (c) 2014-2017 OSharp. All rights reserved.
//  </copyright>
//  <site>http://www.osharp.org</site>
//  <last-editor></last-editor>
//  <last-date>2017-09-17 16:45</last-date>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Microsoft.Extensions.Caching.Distributed;

using OSharp.Data;
using OSharp.Extensions;
using OSharp.Json;


namespace OSharp.Caching
{
    /// <summary>
    /// <see cref="IDistributedCache"/>扩展方法
    /// </summary>
    public static class DistributedCacheExtensions
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> KeyLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// 设置 将对象转换为JSON字符串的委托
        /// </summary>
        public static Func<object, string> ToJsonFunc = val => val.ToJsonString();

        public static Func<string, Type, object> FromJsonFunc = (json, type) => json.FromJsonString(type);

        /// <summary>
        /// 将对象存入缓存中
        /// </summary>
        public static void Set(this IDistributedCache cache, string key, object value, DistributedCacheEntryOptions options = null)
        {
            Check.NotNullOrEmpty(key, nameof(key));
            Check.NotNull(value, nameof(value));

            string json = ToJsonFunc(value);
            if (options == null)
            {
                cache.SetString(key, json);
            }
            else
            {
                cache.SetString(key, json, options);
            }
        }

        /// <summary>
        /// 异步将对象存入缓存中
        /// </summary>
        public static async Task SetAsync(this IDistributedCache cache, string key, object value, DistributedCacheEntryOptions options = null, CancellationToken token = default)
        {
            Check.NotNullOrEmpty(key, nameof(key));
            Check.NotNull(value, nameof(value));

            string json = ToJsonFunc(value);
            if (options == null)
            {
                await cache.SetStringAsync(key, json, token);
            }
            else
            {
                await cache.SetStringAsync(key, json, options, token);
            }
        }

        /// <summary>
        /// 将对象存入缓存中，使用指定时长
        /// </summary>
        public static void Set(this IDistributedCache cache, string key, object value, int cacheSeconds)
        {
            Check.NotNullOrEmpty(key, nameof(key));
            Check.NotNull(value, nameof(value));
            Check.GreaterThan(cacheSeconds, nameof(cacheSeconds), 0);

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions();
            options.SetAbsoluteExpiration(TimeSpan.FromSeconds(cacheSeconds));
            cache.Set(key, value, options);
        }

        /// <summary>
        /// 异步将对象存入缓存中，使用指定时长
        /// </summary>
        public static Task SetAsync(this IDistributedCache cache, string key, object value, int cacheSeconds, CancellationToken token = default)
        {
            Check.NotNullOrEmpty(key, nameof(key));
            Check.NotNull(value, nameof(value));
            Check.GreaterThan(cacheSeconds, nameof(cacheSeconds), 0);

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions();
            options.SetAbsoluteExpiration(TimeSpan.FromSeconds(cacheSeconds));
            return cache.SetAsync(key, value, options, token);
        }

        /// <summary>
        /// 获取指定键的缓存项
        /// </summary>
        public static TResult Get<TResult>(this IDistributedCache cache, string key)
        {
            string json = cache.GetString(key);
            if (json == null)
            {
                return default(TResult);
            }

            return (TResult)FromJsonFunc(json, typeof(TResult));
        }

        /// <summary>
        /// 异步获取指定键的缓存项
        /// </summary>
        public static async Task<TResult> GetAsync<TResult>(this IDistributedCache cache, string key, CancellationToken token = default)
        {
            string json = await cache.GetStringAsync(key, token);
            if (json == null)
            {
                return default(TResult);
            }
            return (TResult)FromJsonFunc(json, typeof(TResult));
        }

        /// <summary>
        /// 获取指定键的缓存项，不存在则从指定委托获取，并回存到缓存中再返回
        /// </summary>
        public static TResult Get<TResult>(this IDistributedCache cache, string key, Func<TResult> getFunc, DistributedCacheEntryOptions options = null)
        {
            TResult result = cache.Get<TResult>(key);
            if (!Equals(result, default(TResult)))
            {
                return result;
            }

            var keyLock = KeyLocks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));
            try
            {
                keyLock.Wait();

                // 双重检查
                result = cache.Get<TResult>(key);
                if (!Equals(result, default(TResult)))
                {
                    return result;
                }

                result = getFunc();
                if (Equals(result, default(TResult)))
                {
                    return default(TResult);
                }
                cache.Set(key, result, options);
                return result;
            }
            finally
            {
                keyLock.Release();
            }
        }

        /// <summary>
        /// 异步获取指定键的缓存项，不存在则从指定委托获取，并回存到缓存中再返回
        /// </summary>
        public static async Task<TResult> GetAsync<TResult>(this IDistributedCache cache,
            string key,
            Func<Task<TResult>> getAsyncFunc,
            DistributedCacheEntryOptions options = null,
            CancellationToken token = default)
        {
            TResult result = await cache.GetAsync<TResult>(key, token);
            if (!Equals(result, default(TResult)))
            {
                return result;
            }

            var keyLock = KeyLocks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));
            try
            {
                await keyLock.WaitAsync(token);

                // 双重检查
                result = await cache.GetAsync<TResult>(key, token);
                if (!Equals(result, default(TResult)))
                {
                    return result;
                }

                result = await getAsyncFunc();
                if (Equals(result, default(TResult)))
                {
                    return default(TResult);
                }
                await cache.SetAsync(key, result, options, token);
                return result;
            }
            finally
            {
                keyLock.Release();
            }
        }

        /// <summary>
        /// 异步获取指定键的缓存项，不存在则从指定委托获取，并回存到缓存中再返回
        /// </summary>
        public static async Task<TResult> GetAsync<TResult>(this IDistributedCache cache,
            string key,
            Func<CancellationToken, Task<TResult>> getAsyncFunc,
            DistributedCacheEntryOptions options = null,
            CancellationToken token = default)
        {
            TResult result = await cache.GetAsync<TResult>(key, token);
            if (!Equals(result, default(TResult)))
            {
                return result;
            }

            var keyLock = KeyLocks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));
            try
            {
                await keyLock.WaitAsync(token);

                // 双重检查
                result = await cache.GetAsync<TResult>(key, token);
                if (!Equals(result, default(TResult)))
                {
                    return result;
                }

                result = await getAsyncFunc(token);
                if (Equals(result, default(TResult)))
                {
                    return default(TResult);
                }
                await cache.SetAsync(key, result, options, token);
                return result;
            }
            finally
            {
                keyLock.Release();
            }
        }

        /// <summary>
        /// 获取指定键的缓存项，不存在则从指定委托获取，并回存到缓存中再返回
        /// </summary>
        public static TResult Get<TResult>(this IDistributedCache cache, string key, Func<TResult> getFunc, int cacheSeconds)
        {
            Check.GreaterThan(cacheSeconds, nameof(cacheSeconds), 0);

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions();
            options.SetAbsoluteExpiration(TimeSpan.FromSeconds(cacheSeconds));
            return cache.Get<TResult>(key, getFunc, options);
        }

        /// <summary>
        /// 异步获取指定键的缓存项，不存在则从指定委托获取，并回存到缓存中再返回
        /// </summary>
        public static Task<TResult> GetAsync<TResult>(this IDistributedCache cache,
            string key,
            Func<Task<TResult>> getAsyncFunc,
            int cacheSeconds,
            CancellationToken token = default)
        {
            Check.GreaterThan(cacheSeconds, nameof(cacheSeconds), 0);

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions();
            options.SetAbsoluteExpiration(TimeSpan.FromSeconds(cacheSeconds));
            return cache.GetAsync<TResult>(key, getAsyncFunc, options, token);
        }

        /// <summary>
        /// 异步获取指定键的缓存项，不存在则从指定委托获取，并回存到缓存中再返回
        /// </summary>
        public static Task<TResult> GetAsync<TResult>(this IDistributedCache cache,
            string key,
            Func<CancellationToken, Task<TResult>> getAsyncFunc,
            int cacheSeconds,
            CancellationToken token = default)
        {
            Check.GreaterThan(cacheSeconds, nameof(cacheSeconds), 0);

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions();
            options.SetAbsoluteExpiration(TimeSpan.FromSeconds(cacheSeconds));
            return cache.GetAsync<TResult>(key, getAsyncFunc, options, token);
        }

        /// <summary>
        /// 移除指定键的缓存项
        /// </summary>
        public static void Remove(this IDistributedCache cache, params string[] keys)
        {
            Check.NotNull(keys, nameof(keys));
            foreach (string key in keys)
            {
                cache.Remove(key);
            }
        }

        /// <summary>
        /// 移除指定键的缓存项
        /// </summary>
        public static async Task RemoveAsync(this IDistributedCache cache, CancellationToken token = default, params string[] keys)
        {
            Check.NotNull(keys, nameof(keys));
            foreach (string key in keys)
            {
                await cache.RemoveAsync(key, token);
            }
        }

    }
}
