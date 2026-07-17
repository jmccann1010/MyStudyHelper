using Microsoft.AspNetCore.Http;

namespace StudyHelper.Tests.Helpers;

/// <summary>
/// In-memory ISession implementation for controller unit tests.
/// Prevents InvalidOperationException from DefaultHttpContext.Session when no
/// session middleware is configured, and allows tests to pre-seed session values.
/// </summary>
internal sealed class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new(StringComparer.Ordinal);

    public bool IsAvailable => true;
    public string Id => "test-session-id";
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
}
