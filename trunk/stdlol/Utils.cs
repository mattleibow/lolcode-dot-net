using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace stdlol
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

        public static bool ToBool(object b)
        {
            if (b == null)
                return false;
            if (b is int && ((int)b) == 0)
                return false;
            if (b is float && ((float)b) == 0)
                return false;
            if (b is string && ((string)b) == "")
                return false;
            if (b is bool && !(bool)b)
                return false;
            return true;
        }

        /*public static object GetObject(Dictionary<object, object> dict, object key)
        {
            object ret;
            dict.TryGetValue(key, out ret);
            return ret;
        }

        public static Dictionary<object, object> GetDict(Dictionary<object, object> dict, object key)
        {
            object obj;
            Dictionary<object, object> ret;

            if (!dict.TryGetValue(key, out obj))
                obj = null;

            if (obj is Dictionary<object, object>)
            {
                return obj as Dictionary<object, object>;
            }
            else if (obj == null)
            {
                ret = new Dictionary<object, object>();
                dict[key] = ret;
                return ret;
            }
            else if (obj is string || obj is int || obj is float || obj is bool)
            {
                ret = new Dictionary<object, object>();
                ret[0] = obj;
                dict[key] = ret;
                return ret;
            }

            throw new InvalidCastException(string.Format("Unknown type \"{0}\"", obj.GetType().Name));
        }

        public static Dictionary<object, object> ToDict(ref object obj)
        {
            Dictionary<object, object> ret;

            if (obj is Dictionary<object, object>)
            {
                return obj as Dictionary<object, object>;
            }
            else if (obj == null)
            {
                ret = new Dictionary<object, object>();
                obj = ret;
                return ret;
            }
            else if (obj is string || obj is int || obj is float || obj is bool)
            {
                ret = new Dictionary<object, object>();
                ret[0] = obj;
                obj = ret;
                return ret;
            }

            throw new InvalidCastException(string.Format("Unknown type \"{0}\"", obj.GetType().Name));
        }*/

        public static string ToString(object obj)
        {
            if(obj == null)
                throw new InvalidCastException("Cannot cast NOOB to string");
            /*if (obj is Dictionary<object, object>)
                return ToString(GetObject(obj as Dictionary<object, object>, 0));*/
            return obj.ToString();
        }

        public static int ToInt(object obj)
        {
            if (obj is int)
                return (int)obj;
            if (obj is float)
                return (int)(float)obj;
            if (obj is bool)
                return ((bool)obj) ? 1 : 0;
            /*if (obj is Dictionary<object, object>)
                return ToInt(GetObject(obj as Dictionary<object, object>, 0));*/
            if (obj is string)
            {
                int val;
                if (!int.TryParse(obj as string, out val))
                    throw new InvalidCastException("Cannot cast non-numeric YARN to NUMBR");
                return val;
            }

            throw new InvalidCastException(string.Format("Cannot cast type \"{0}\" to NUMBR", obj.GetType().Name));
        }

        public static float ToFloat(object obj)
        {
            if (obj is int)
                return (int)obj;
            if (obj is float)
                return (float)obj;
            if (obj is bool)
                return ((bool)obj) ? 1 : 0;
            /*if (obj is Dictionary<object, object>)
                return ToInt(GetObject(obj as Dictionary<object, object>, 0));*/
            if (obj is string)
            {
                float val;
                if (!float.TryParse(obj as string, out val))
                    throw new InvalidCastException("Cannot cast non-numeric YARN to NUMBAR");
                return val;
            }

            throw new InvalidCastException(string.Format("Cannot cast type \"{0}\" to NUMBAR", obj.GetType().Name));
        }

        public static void PrintObject(TextWriter writer, object obj, bool newline)
        {
            string pattern;
            if (newline)
            {
                pattern = "{0}" + Environment.NewLine;
            }
            else
            {
                pattern = "{0}";
            }

            if (obj.GetType() != typeof(Dictionary<object, object>))
            {
                writer.Write(pattern, obj);
            }
            else
            {
                Dictionary<object, object> dict = obj as Dictionary<object, object>;
                foreach (object o2 in dict.Values)
                    writer.Write(pattern, o2);
            }
        }
    }
}
