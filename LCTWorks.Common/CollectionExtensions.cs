using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCTWorks.Common
{
    public static class CollectionExtensions
    {
        public static IEnumerable<KeyValuePair<string, string>> ToValidStringKeyValuePair(this IEnumerable<(string Key, string Value)> items)
        {
            if (items == null)
            {
                return [];
            }
            return items.Select(x =>
            {
                var validKey = string.IsNullOrEmpty(x.Key) ? Guid.NewGuid().ToString() : x.Key;
                var validVal = string.IsNullOrEmpty(x.Value) ? "-" : x.Value;
                return new KeyValuePair<string, string>(validKey, validVal);
            });
        }
    }
}