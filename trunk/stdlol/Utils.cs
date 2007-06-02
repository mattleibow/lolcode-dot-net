using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace notdot.LOLCode.stdlol
{
    public abstract class Utils
    {
        public static string ReadWord(TextReader reader)
        {
            StringBuilder sb = new StringBuilder();
            int c;
            while (true)
            {
                c = reader.Read();
                if (c == -1)
                    return "";
                if (!char.IsWhiteSpace((char)c))
                    break;
            }

            sb.Append(c);
            while (true)
            {
                c = reader.Read();
                if (c == -1 || char.IsWhiteSpace((char)c))
                    return sb.ToString();
                sb.Append(c);
            }
        }

        public static object GetObject(Dictionary<object, object> dict, object key)
        {
            object ret;
            dict.TryGetValue(key, out ret);
            return ret;
        }

        public static object GetDict(Dictionary<object, object> dict, object key)
        {
            object obj;
            Dictionary<object, object> ret;
            dict.TryGetValue(key, out obj);
            if (obj is Dictionary<object, object>)
                return obj as Dictionary<object, object>;
            ret = ToDict(obj);
            dict[key] = ret;
            return ret;
        }

        public static Dictionary<object, object> ToDict(object obj)
        {
            if (obj is Dictionary<object, object>)
                return obj as Dictionary<object, object>;
            if (obj == null)
                return new Dictionary<object, object>();
            if (obj is string || obj is int)
            {
                Dictionary<object, object> ret = new Dictionary<object, object>();
                ret[0] = obj;

                return ret;
            }

            throw new InvalidCastException(string.Format("Unknown type \"{0}\"", obj.GetType().Name));
        }

        public static string ToString(object obj)
        {
            if (obj is string)
                return obj as string;
            if (obj == null)
                return "";
            if (obj is Dictionary<object, object>)
                return ToString(GetObject(obj as Dictionary<object,object>, 0));
            if (obj is int)
                return ((int)obj).ToString();

            throw new InvalidCastException(string.Format("Cannot cast type \"{0}\" to string", obj.GetType().Name));
        }

        public static int ToInt(object obj)
        {
            if (obj is int)
                return (int)obj;
            if (obj == null)
                return 0;
            if (obj is Dictionary<object, object>)
                return ToInt(GetObject(obj as Dictionary<object, object>, 0));

            throw new InvalidCastException(string.Format("Cannot cast type \"{0}\" to int", obj.GetType().Name));
        }
    }
}
