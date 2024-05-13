using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedIdempotency.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]

    public class IdempotencyKeyAttribute : Attribute
    {
        public int? Order { get; set; }
        public IdempotencyKeyAttribute(int order)
        {
            Order = order;
        }
        public IdempotencyKeyAttribute()
        {
        }
    }
}
