using DistributedIdempotency.Behaviours;

namespace DistributedIdempotency.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class IdempotentAttribute : Attribute
    {
        public string KeyExtractorNamespace { get; }
        public string KeyExtractorMethodName { get; }
        public string KeyExtractorClassName { get; }
        /// <summary>
        /// How long (in milliseconds) a duplicate request should wait for a response before returning a conflict status code
        /// </summary>
        public int TimeOut { get; }

        /// <summary>
        /// The maximum duration(milliseconds) apart transactions needs to be before being considered as a duplicate
        /// </summary>
        public int Window { get; }

        public IdempotentAttribute(string keyExtractorNamespace, string keyExtractorClassName, string keyExtractorMethodName, int windowInMilliseconds = 300000, int timeOutInMilliseconds = 60000)
        {
            KeyExtractorNamespace = keyExtractorNamespace;
            KeyExtractorMethodName = keyExtractorMethodName;
            KeyExtractorClassName = keyExtractorClassName;
        }

        public IdempotentAttribute(string keyExtractorMethodName, int windowInMilliseconds = 300000, int timeOutInMilliseconds = 60000) : this(typeof(IdempotencyInterceptor).Namespace,nameof(IdempotencyInterceptor), keyExtractorMethodName, windowInMilliseconds, timeOutInMilliseconds)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="windowInMilliseconds">The maximum duration apart transactions needs to be before being considered as a duplicate. Default is 5mins</param>
        /// <param name="timeOutInMilliseconds">How long a duplicate request should wait for a response before returning a conflict status code. Default is 1min</param>
        public IdempotentAttribute(int windowInMilliseconds = 300000, int timeOutInMilliseconds = 60000)
        {
            Window = windowInMilliseconds;
            TimeOut = timeOutInMilliseconds;
        }
    }

}
