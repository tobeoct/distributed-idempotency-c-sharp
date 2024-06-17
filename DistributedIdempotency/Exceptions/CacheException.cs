using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedIdempotency.Exceptions
{
    internal class CacheException:Exception
    {
        public CacheException(string message): base(message: message)
        {

        }
    }
}
