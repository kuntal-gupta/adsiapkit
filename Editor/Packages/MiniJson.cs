using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AdsIAPKit.Editor
{
    /// <summary>
    /// Small JSON reader and writer for Packages/manifest.json, which JsonUtility cannot round-trip.
    /// Objects become Dictionary&lt;string, object&gt; (key order kept), arrays List&lt;object&gt;.
    /// </summary>
    internal static class MiniJson
    {
        public static object Parse(string json)
        {
            var parser = new Parser(json);
            var value = parser.ReadValue();
            parser.SkipWhitespace();
            if (!parser.AtEnd) throw parser.Error("Unexpected trailing characters");
            return value;
        }

        public static string Serialize(object value)
        {
            var sb = new StringBuilder();
            Write(sb, value, 0);
            return sb.ToString();
        }

        // ── Writer ─────────────────────────────────────────────────────────────
        private static void Write(StringBuilder sb, object value, int indent)
        {
            switch (value)
            {
                case null:   sb.Append("null"); break;
                case string s: WriteString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case double d: sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); break;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                case int i:  sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
                case IDictionary<string, object> obj:
                    if (obj.Count == 0) { sb.Append("{}"); break; }
                    sb.Append("{\n");
                    var first = true;
                    foreach (var pair in obj)
                    {
                        if (!first) sb.Append(",\n");
                        first = false;
                        sb.Append(' ', (indent + 1) * 2);
                        WriteString(sb, pair.Key);
                        sb.Append(": ");
                        Write(sb, pair.Value, indent + 1);
                    }
                    sb.Append('\n').Append(' ', indent * 2).Append('}');
                    break;
                case IList list:
                    if (list.Count == 0) { sb.Append("[]"); break; }
                    sb.Append("[\n");
                    for (var n = 0; n < list.Count; n++)
                    {
                        if (n > 0) sb.Append(",\n");
                        sb.Append(' ', (indent + 1) * 2);
                        Write(sb, list[n], indent + 1);
                    }
                    sb.Append('\n').Append(' ', indent * 2).Append(']');
                    break;
                default:
                    WriteString(sb, Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
            }
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // ── Reader ─────────────────────────────────────────────────────────────
        private sealed class Parser
        {
            private readonly string _json;
            private int _pos;

            public Parser(string json) { _json = json ?? ""; }

            public bool AtEnd => _pos >= _json.Length;

            public FormatException Error(string message) => new FormatException($"{message} at position {_pos}.");

            public void SkipWhitespace()
            {
                while (!AtEnd && char.IsWhiteSpace(_json[_pos])) _pos++;
            }

            public object ReadValue()
            {
                SkipWhitespace();
                if (AtEnd) throw Error("Unexpected end of JSON");
                switch (_json[_pos])
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': Expect("true");  return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null");  return null;
                    default:  return ReadNumber();
                }
            }

            private Dictionary<string, object> ReadObject()
            {
                var obj = new Dictionary<string, object>();
                _pos++; // {
                SkipWhitespace();
                if (Peek('}')) { _pos++; return obj; }
                while (true)
                {
                    SkipWhitespace();
                    if (!Peek('"')) throw Error("Expected a property name");
                    var key = ReadString();
                    SkipWhitespace();
                    if (!Peek(':')) throw Error("Expected ':'");
                    _pos++;
                    obj[key] = ReadValue();
                    SkipWhitespace();
                    if (Peek(',')) { _pos++; continue; }
                    if (Peek('}')) { _pos++; return obj; }
                    throw Error("Expected ',' or '}'");
                }
            }

            private List<object> ReadArray()
            {
                var list = new List<object>();
                _pos++; // [
                SkipWhitespace();
                if (Peek(']')) { _pos++; return list; }
                while (true)
                {
                    list.Add(ReadValue());
                    SkipWhitespace();
                    if (Peek(',')) { _pos++; continue; }
                    if (Peek(']')) { _pos++; return list; }
                    throw Error("Expected ',' or ']'");
                }
            }

            private string ReadString()
            {
                var sb = new StringBuilder();
                _pos++; // opening quote
                while (!AtEnd)
                {
                    var c = _json[_pos++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) break;
                    var e = _json[_pos++];
                    switch (e)
                    {
                        case '"':  sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/':  sb.Append('/'); break;
                        case 'b':  sb.Append('\b'); break;
                        case 'f':  sb.Append('\f'); break;
                        case 'n':  sb.Append('\n'); break;
                        case 'r':  sb.Append('\r'); break;
                        case 't':  sb.Append('\t'); break;
                        case 'u':
                            if (_pos + 4 > _json.Length) throw Error("Bad unicode escape");
                            sb.Append((char)Convert.ToInt32(_json.Substring(_pos, 4), 16));
                            _pos += 4;
                            break;
                        default: throw Error("Bad escape");
                    }
                }
                throw Error("Unterminated string");
            }

            private object ReadNumber()
            {
                var start = _pos;
                while (!AtEnd && "+-0123456789.eE".IndexOf(_json[_pos]) >= 0) _pos++;
                var text = _json.Substring(start, _pos - start);
                if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) return l;
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
                throw Error("Bad value");
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(_json, _pos, word, 0, word.Length) != 0) throw Error("Bad value");
                _pos += word.Length;
            }

            private bool Peek(char c) => !AtEnd && _json[_pos] == c;
        }
    }
}
