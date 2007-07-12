using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.IO.Compression;

namespace notdot.LOLCode
{
    internal abstract class UnicodeNameLookup
    {
        private static Dictionary<string, string> names = null;

        private static void LoadDictionary()
        {
            names = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            BinaryReader br = new BinaryReader(new GZipStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("notdot.LOLCode.UnicodeNames.dat"), CompressionMode.Decompress));
            
            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
                names.Add(br.ReadString(), br.ReadString());

            br.BaseStream.Close();
        }

        public static string GetUnicodeCharacter(string name)
        {
            if (names == null)
                LoadDictionary();
            string val;
            if (!names.TryGetValue(name, out val))
                return null;
            return val;
        }
    }
}
