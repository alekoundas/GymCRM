using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// A cache that can be read and filled by hand, and says what was looked for.
    /// </summary>
    internal sealed class FakeCache : IDistributedCache
    {
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();

        public List<string> Lines { get; } = new List<string>();

        public byte[]? Get(string key)
        {
            Lines.Add("get " + key);

            return Values.TryGetValue(key, out string? value) ? Encoding.UTF8.GetBytes(value) : null;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Lines.Add("set " + key);
            Values[key] = Encoding.UTF8.GetString(value);
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            throw new NotSupportedException();
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            throw new NotSupportedException();
        }

        public void Refresh(string key)
        {
            throw new NotSupportedException();
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            throw new NotSupportedException();
        }

        public void Remove(string key)
        {
            throw new NotSupportedException();
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            throw new NotSupportedException();
        }
    }
}
