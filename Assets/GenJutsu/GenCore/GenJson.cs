using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GenJutsu.GenCore
{
    public abstract class JVal
    {
        public string Raw = "";
    }

    public sealed class JObj : JVal
    {
        public readonly List<KeyValuePair<string, JVal>> Items = new List<KeyValuePair<string, JVal>>();

        public bool TryGet(string key, out JVal value)
        {
            for (var i = 0; i < Items.Count; i++)
            {
                if (Items[i].Key == key)
                {
                    value = Items[i].Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public JObj Obj(string key)
        {
            return TryGet(key, out var v) ? v as JObj : null;
        }

        public string Str(string key)
        {
            return TryGet(key, out var v) && v is JStr s ? s.Value : null;
        }

        public bool Num(string key, out double value)
        {
            value = 0;
            return TryGet(key, out var v) && v is JNum n && n.Try(out value);
        }
    }

    public sealed class JArr : JVal
    {
        public readonly List<JVal> Items = new List<JVal>();
    }

    public sealed class JStr : JVal
    {
        public string Value = "";
    }

    public sealed class JNum : JVal
    {
        public double Value;

        public bool Try(out double value)
        {
            value = Value;
            return true;
        }
    }

    public sealed class JBool : JVal
    {
        public bool Value;
    }

    public sealed class JNull : JVal
    {
    }

    /// <summary>
    /// Small JSON reader that keeps each value's original slice so tool args can be forwarded unchanged.
    /// </summary>
    public static class GenJson
    {
        public static bool TryParse(string json, out JVal value)
        {
            value = null;
            if (string.IsNullOrEmpty(json))
                return false;
            var p = new Parser(json);
            if (!p.TryValue(out value))
                return false;
            p.Skip();
            return p.Done;
        }

        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            var sb = new StringBuilder(text.Length + 8);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>Gemini function responses must be a JSON object.</summary>
        public static string AsObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "{\"ok\":false}";
            var t = json.Trim();
            if (t.StartsWith("{", StringComparison.Ordinal))
                return t;
            if (t.StartsWith("[", StringComparison.Ordinal) || t.StartsWith("\"", StringComparison.Ordinal)
                || t == "true" || t == "false" || t == "null" || LooksLikeNumber(t))
                return "{\"result\":" + t + "}";
            return "{\"result\":\"" + Escape(t) + "\"}";
        }

        static bool LooksLikeNumber(string t)
        {
            return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        sealed class Parser
        {
            readonly string s;
            int i;

            public Parser(string s)
            {
                this.s = s;
            }

            public bool Done => i >= s.Length;

            public void Skip()
            {
                while (i < s.Length && char.IsWhiteSpace(s[i]))
                    i++;
            }

            public bool TryValue(out JVal value)
            {
                Skip();
                value = null;
                if (i >= s.Length)
                    return false;
                var start = i;
                var c = s[i];
                if (c == '{')
                    value = ReadObj();
                else if (c == '[')
                    value = ReadArr();
                else if (c == '"')
                    value = ReadStr();
                else if (c == 't' || c == 'f')
                    value = ReadBool();
                else if (c == 'n')
                    value = ReadNull();
                else if (c == '-' || char.IsDigit(c))
                    value = ReadNum();
                else
                    return false;
                if (value == null)
                    return false;
                value.Raw = s.Substring(start, i - start);
                return true;
            }

            JObj ReadObj()
            {
                i++;
                var obj = new JObj();
                Skip();
                if (i < s.Length && s[i] == '}')
                {
                    i++;
                    return obj;
                }

                while (i < s.Length)
                {
                    Skip();
                    if (i >= s.Length || s[i] != '"')
                        return null;
                    var key = ReadStr();
                    if (key == null)
                        return null;
                    Skip();
                    if (i >= s.Length || s[i] != ':')
                        return null;
                    i++;
                    if (!TryValue(out var child))
                        return null;
                    obj.Items.Add(new KeyValuePair<string, JVal>(key.Value, child));
                    Skip();
                    if (i < s.Length && s[i] == ',')
                    {
                        i++;
                        continue;
                    }

                    if (i < s.Length && s[i] == '}')
                    {
                        i++;
                        return obj;
                    }

                    return null;
                }

                return null;
            }

            JArr ReadArr()
            {
                i++;
                var arr = new JArr();
                Skip();
                if (i < s.Length && s[i] == ']')
                {
                    i++;
                    return arr;
                }

                while (i < s.Length)
                {
                    if (!TryValue(out var child))
                        return null;
                    arr.Items.Add(child);
                    Skip();
                    if (i < s.Length && s[i] == ',')
                    {
                        i++;
                        continue;
                    }

                    if (i < s.Length && s[i] == ']')
                    {
                        i++;
                        return arr;
                    }

                    return null;
                }

                return null;
            }

            JStr ReadStr()
            {
                if (i >= s.Length || s[i] != '"')
                    return null;
                i++;
                var sb = new StringBuilder();
                while (i < s.Length)
                {
                    var c = s[i++];
                    if (c == '"')
                        return new JStr { Value = sb.ToString() };
                    if (c != '\\')
                    {
                        sb.Append(c);
                        continue;
                    }

                    if (i >= s.Length)
                        return null;
                    var e = s[i++];
                    switch (e)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            sb.Append(e);
                            break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 > s.Length)
                                return null;
                            if (!int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var cp))
                                return null;
                            sb.Append((char)cp);
                            i += 4;
                            break;
                        default:
                            return null;
                    }
                }

                return null;
            }

            JBool ReadBool()
            {
                if (s.IndexOf("true", i, StringComparison.Ordinal) == i)
                {
                    i += 4;
                    return new JBool { Value = true };
                }

                if (s.IndexOf("false", i, StringComparison.Ordinal) == i)
                {
                    i += 5;
                    return new JBool { Value = false };
                }

                return null;
            }

            JNull ReadNull()
            {
                if (s.IndexOf("null", i, StringComparison.Ordinal) != i)
                    return null;
                i += 4;
                return new JNull();
            }

            JNum ReadNum()
            {
                var start = i;
                if (s[i] == '-')
                    i++;
                while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E' || s[i] == '+' || s[i] == '-'))
                    i++;
                var raw = s.Substring(start, i - start);
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
                    return null;
                return new JNum { Value = n };
            }
        }
    }
}
