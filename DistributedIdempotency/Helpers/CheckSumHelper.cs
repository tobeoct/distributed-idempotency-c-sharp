using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DistributedIdempotency.Helpers
{
    internal class ChecksumHelper
    {
        public static string GetMD5Checksum(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Object cannot be null.");

            string serializedObject = SerializeObject(obj);

            // Calculate the MD5 checksum
            using (MD5 md5 = MD5.Create())
            {
                byte[] checksumBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(serializedObject));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in checksumBytes)
                {
                    sb.Append(b.ToString("x2")); // Convert each byte to hexadecimal format
                }
                return sb.ToString();
            }
        }

        private static string SerializeObject(object obj)
        {
            return JsonSerializer.Serialize(obj);
        }
    }
}
