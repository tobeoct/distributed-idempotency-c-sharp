

namespace DistributedIdempotency.Helpers
{
    internal class Configuration
    {
        public const string Key = "DistributedIdempotency";
        public bool StrictMode { get; set; } = true;
    }
}
