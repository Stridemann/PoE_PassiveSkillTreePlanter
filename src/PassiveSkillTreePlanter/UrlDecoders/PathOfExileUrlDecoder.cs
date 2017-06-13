using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassiveSkillTreePlanter.UrlDecoders
{
    public class PathOfExileUrlDecoder
    {
        public static List<ushort> Decode(string url)
        {
            List<ushort> nodesId = new List<ushort>();
            
            var textToDecode = url.Substring(url.IndexOf("tree/") + 5).Replace("-", "+").Replace("_", "/");

            byte[] data = Convert.FromBase64String(textToDecode);

            var Version = data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3];
            var Class = data[4];
            var aClass = data[5];

            for (int k = (Version > 3 ? 7 : 6); k < data.Length; k += 2)
            {
                ushort nodeId = (ushort)(data[k] << 8 | data[k + 1]);
                nodesId.Add(nodeId);
            }

            return nodesId;
        }
    }
}
