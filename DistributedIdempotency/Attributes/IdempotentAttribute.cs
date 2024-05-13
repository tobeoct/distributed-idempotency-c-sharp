using DistributedIdempotency.Behaviours;

namespace DistributedIdempotency.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
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
        public bool StrictMode { get; }

        //public IdempotentAttribute(string keyExtractorNamespace, string keyExtractorClassName, string keyExtractorMethodName, int windowInMilliseconds = 300000, int timeOutInMilliseconds = 60000, bool strictMode=false)
        //{
        //    KeyExtractorNamespace = keyExtractorNamespace;
        //    KeyExtractorMethodName = keyExtractorMethodName;
        //    KeyExtractorClassName = keyExtractorClassName;
        //    StrictMode = strictMode;
        //}

        //public IdempotentAttribute(string keyExtractorMethodName, int windowInMilliseconds = 300000, int timeOutInMilliseconds = 60000, bool strictMode = false) : this(typeof(IdempotencyInterceptor).Namespace,nameof(IdempotencyInterceptor), keyExtractorMethodName, windowInMilliseconds, timeOutInMilliseconds, strictMode)
        //{
        //    StrictMode = strictMode;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="windowInMilliseconds">The maximum duration apart transactions needs to be before being considered as a duplicate. Default is 5mins</param>
        /// <param name="timeOutInMilliseconds">How long a duplicate request should wait for a response before returning a conflict status code. Default is 1min</param>
        public IdempotentAttribute(int windowInMilliseconds = 300000, int timeOutInMilliseconds = 60000, bool strictMode = true)
        {
            Window = windowInMilliseconds;
            TimeOut = timeOutInMilliseconds;
            StrictMode = strictMode;
        }
    }

}
