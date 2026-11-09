using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace LabTrack.Tests.Soporte
{
    public sealed class SesionFalsa : ISession
    {
        private readonly Dictionary<string, byte[]> _valores = new();

        public bool IsAvailable => true;
        public string Id { get; } = "sesion-prueba";
        public IEnumerable<string> Keys => _valores.Keys;
        public void Clear() => _valores.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _valores.Remove(key);
        public void Set(string key, byte[] value) => _valores[key] = value;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _valores.TryGetValue(key, out value);
    }
}
