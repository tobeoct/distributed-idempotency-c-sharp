using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedIdempotency.Helpers
{
    internal class Env
    {
        public static Configuration AppSettings;

        public static void Configure(IOptions<Configuration> options)
        {
            AppSettings = options.Value?? new Configuration();
        }

    }
}
