using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedIdempotency.Exceptions
{
    public class IdempotencyException:Exception
    {
        public IdempotencyException(string message) : base(message: message)
        {

        }
    }
}
