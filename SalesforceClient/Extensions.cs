using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Prettifies a Json String (extends string). See: https://stackoverflow.com/questions/4580397/json-formatter-in-c
/// </summary>
namespace MyOrg.Client.Salesforce {
  public static class ResponseHelper {
    private const string INDENT_STRING = "    ";

    public static string ExtractResultId(this string str) {
      int startPosition = str.IndexOf("\"") + 1;
      int endPosition = str.LastIndexOf("\"");
      return str.Substring(startPosition, endPosition - startPosition);
    }
    
    /// <summary>
    /// Chunk list. For example, if you chuck a list of 18 items by 5 items per chunk, this will return a list of 4 sublists with the following items inside: 5-5-5-3.
    /// See: https://stackoverflow.com/questions/11463734/split-a-list-into-smaller-lists-of-n-size
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="chunkSize"></param>
    /// <returns></returns>
    public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize) {
      return source
          .Select((x, i) => new { Index = i, Value = x })
          .GroupBy(x => x.Index / chunkSize)
          .Select(x => x.Select(v => v.Value).ToList())
          .ToList();
    }

    public static string PrettifyJson(this string str) {
      var indent = 0;
      var quoted = false;
      var sb = new StringBuilder();
      for (var i = 0; i < str.Length; i++) {
        var ch = str[i];
        switch (ch) {
          case '{':
          case '[':
            sb.Append(ch);
            if (!quoted) {
              sb.AppendLine();
              Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
            }
            break;
          case '}':
          case ']':
            if (!quoted) {
              sb.AppendLine();
              Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
            }
            sb.Append(ch);
            break;
          case '"':
            sb.Append(ch);
            bool escaped = false;
            var index = i;
            while (index > 0 && str[--index] == '\\')
              escaped = !escaped;
            if (!escaped)
              quoted = !quoted;
            break;
          case ',':
            sb.Append(ch);
            if (!quoted) {
              sb.AppendLine();
              Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
            }
            break;
          case ':':
            sb.Append(ch);
            if (!quoted)
              sb.Append(" ");
            break;
          default:
            sb.Append(ch);
            break;
        }
      }
      return sb.ToString();
    }
  }

  public static class Extensions {
    public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action) {
      foreach (var i in ie) {
        action(i);
      }
    }
  }
}
