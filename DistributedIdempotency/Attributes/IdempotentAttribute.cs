using DistributedIdempotency.Helpers;

namespace DistributedIdempotency.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class IdempotentAttribute : Attribute
    {
        /// <summary>
        /// How long (in milliseconds) a duplicate request should wait for a response before returning a conflict status code
        /// </summary>
        public int TimeOut { get; }

        /// <summary>
        /// The maximum duration(milliseconds) apart transactions needs to be before being considered as a duplicate
        /// </summary>
        public int Window { get; }
        public bool StrictMode { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="windowInMilliseconds">The maximum duration apart transactions needs to be before being considered as a duplicate. Default is 5mins</param>
        /// <param name="timeOutInMilliseconds">How long a duplicate request should wait for a response before returning a conflict status code. Default is 1min</param>
        public IdempotentAttribute(int duplicateWindowInMilliseconds = 300000, int timeOutInMilliseconds = 60000)
        {
            Window = duplicateWindowInMilliseconds;
            TimeOut = timeOutInMilliseconds;
            StrictMode = Configuration.StrictMode;
        }
        public IdempotentAttribute(bool strictMode, int windowInMilliseconds = 300000, int timeOutInMilliseconds = 60000)
        {
            Window = windowInMilliseconds;
            TimeOut = timeOutInMilliseconds;
            StrictMode = strictMode;
        }
    }

}
