using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LuaParser
{
    /// <summary>
    /// Класс для конвертаций Lua таблицы с класс C#
    /// </summary>
    public static class LuaParser
    {
        /// <summary>
        /// Событие завершения парсинга одного значения
        /// </summary>
        public static event Action<LuaValue> ParseEvent;

        /// <summary>
        /// Событие завершения парсинга дынных
        /// </summary>
        public static event Action CompliteEvent;

        static Dictionary<Guid, string> functions;

        /// <summary>
        /// Конертация значений
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static async void ParseAsync(string data)
        {
            data = RemoveComments(data);

            functions = new Dictionary<Guid, string>();

            var lines = SplitData(data);

            List<LuaToken> tokens = StringsToTokens(lines);

            await Task.Run(() => TokensToLuaValue(tokens));
        }

        public static List<LuaToken> GetLuaTokens(string data)
        {
            data = RemoveComments(data);

            var lines = SplitData(data);

            return StringsToTokens(lines);
        }

        /// <summary>
        /// Удаление комментариев из lua таблицы 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string RemoveComments(string data)
        {
            while (data.IndexOf("--") > -1)
            {
                var start = data.IndexOf("--");
                var end = data.IndexOf("\n", start);

                data = data.Remove(start, end - start);
            }

            return data;
        }

        public static string RemoveFunc(string data, out Dictionary<Guid, string> removedFunctions)
        {
            removedFunctions = new Dictionary<Guid, string>();

            while (data.Contains('(') && data.Contains(')'))
            {
                Guid guid = Guid.NewGuid();
                string func;

                data = LuaParserExtension.FromToDivide(data, '(', ')', out func, guid.ToString().Replace("-", "_"));

                removedFunctions.Add(guid, func);
            }

            // Удаляем оставшиеся функий без ключей: { test='homo' },somefunction,{ index=0 } -> { test='homo' },{ index=0 } 
            {
                Regex regex = new Regex(@"[,]+[\w]*[,]+");
                MatchCollection matches = regex.Matches(data);

                List<Match> matchesRevers = new List<Match>();

                foreach (Match match in matches)
                {
                    matchesRevers.Insert(0, match);
                }
                matchesRevers.ForEach(e =>
                {
                    data = data
                        .Remove(e.Index, e.Value.Length/*-1*/)
                        .Insert(e.Index, ",");
                }
                );
            }

            return data;
        }

        /// <summary>
        /// Разделение значение Lua таблицы для конвертирования их в токены
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static string[] SplitData(string data)
        {
            var nLine = '\n';

            // допустим
            {
                data = data
                   .Replace("{}", "{void = 0}");
            }

            // удаляем пробелы и некоторые переносы
            {
                data = data
                    .Replace(" ", string.Empty)
                    .Replace($"{(char)9}", string.Empty)
                    .Replace("\n", string.Empty)
                    .Replace(Environment.NewLine, string.Empty);
            }

            // удаляем функций
            {
                data = RemoveFunc(data, out Dictionary<Guid, string> partFunctions);

                partFunctions.ToList().ForEach(e => functions.Add(e.Key, e.Value));
            }

            // Добавляем ключи элемента не имеющим их {{ any = 'desk' }} -> { element = { any = 'desk' }}
            {
                data = data
                    .Replace("{{", "{ element={")
                    .Replace("},{", "}, element={");
            }

            // какая гнида записывает массив так: {2, }
            {
                data = data
                    .Replace(",}", "}");
            }

            // simple split
            {
                data = data
                    .Replace("{", "{" + nLine)
                 .Replace("}", nLine + "}")
                 .Replace("=", nLine + "=" + nLine);
            }

            // исправиляем проблему с линией 'key = someFunction(parm1, param2),'
            {
                int index = 0;

                while (data.IndexOf(',', index) > 0)
                {
                    index = data.IndexOf(',', index);

                    if (data.LastIndexOf('(', index) < data.LastIndexOf('=', index))
                        data = data.Insert(index, nLine.ToString()).Insert(index + 2, nLine.ToString());

                    index += 2;

                    if (index >= data.Length)
                        break;
                }

                data = data.Replace("),", $"){nLine},{nLine}");
            }

            return data.Split(new char[1] { nLine });
        }

        /// <summary>
        /// Конвертация линий Lua таблицы в токены 
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        static List<LuaToken> StringsToTokens(string[] lines)
        {
            List<LuaToken> tokens = new List<LuaToken>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf("--") != -1)
                    lines[i] = lines[i].Substring(0, lines[i].IndexOf("--"));

                switch (lines[i])
                {
                    case "":
                        continue;
                    case "{":
                        tokens.Add(new LuaToken() { type = LuaTokenType.start });
                        continue;
                    case "}":
                        tokens.Add(new LuaToken() { type = LuaTokenType.end });
                        continue;
                    case "=":
                        tokens.Add(new LuaToken() { type = LuaTokenType.key, value = lines[i - 1] });
                        tokens.Add(new LuaToken() { type = LuaTokenType.assign });
                        continue;
                    case ",":
                        tokens.Add(new LuaToken() { type = LuaTokenType.endValue });
                        continue;
                    default:
                        break;
                }

                if (lines[i + 1] == "=")
                    continue;

                tokens.Add(new LuaToken() { type = LuaTokenType.value, value = lines[i] });

                if (lines[i + 1] != ",")
                    tokens.Add(new LuaToken() { type = LuaTokenType.endValue });
            }

            return tokens;
        }

        /// <summary>
        /// Конвертация токенов в класс для работы
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        static void TokensToLuaValue(List<LuaToken> tokens)
        {
            LuaValue current = new LuaValue();

            void AddRoot()
            {
                var parent = current;

                current = new LuaValue();

                parent.AddChildren(current);
            }

            void BackRoot()
            {
                current = current.parent;

                if (current.parent == null)
                {
                    var last = current.AllChildren.Last();
                    last.GetMyFunctions(functions);
                    ParseEvent?.Invoke(last);
                }
            }

            LuaTokenType pastTokenType = LuaTokenType.None;

            bool isComplexValue = false;

            foreach (var token in tokens)
            {
                switch (token.type)
                {
                    case LuaTokenType.start:
                        switch (pastTokenType)
                        {
                            case LuaTokenType.start:
                            case LuaTokenType.endValue:
                                isComplexValue = false;
                                AddRoot();
                                break;
                            case LuaTokenType.assign:
                                isComplexValue = true;
                                break;
                        }
                        break;
                    case LuaTokenType.end:
                        switch (pastTokenType)
                        {
                            case LuaTokenType.end:
                                BackRoot();
                                break;
                        }
                        isComplexValue = false;
                        break;
                    case LuaTokenType.endValue:
                        if (!isComplexValue)
                            BackRoot();
                        break;
                    case LuaTokenType.key:
                        isComplexValue = false;
                        AddRoot();
                        current.key = token.value;
                        break;
                    case LuaTokenType.assign:
                        break;
                    case LuaTokenType.value:
                        current.AddValue(token.value);
                        break;
                    default:
                        break;
                }

                pastTokenType = token.type;
            }

            CompliteEvent?.Invoke();
        }

        public struct LuaToken
        {
            /// <summary>
            /// Тип
            /// </summary>
            public LuaTokenType type;
            /// <summary>
            /// значение
            /// </summary>
            public string value;
        }

        public enum LuaTokenType
        {
            /// <summary>
            /// Начало таблицы
            /// </summary>
            start,
            /// <summary>
            /// Конец таблицы
            /// </summary>
            end,
            /// <summary>
            /// Конец значения
            /// </summary>
            endValue,
            /// <summary>
            /// Ключ значения
            /// </summary>
            key,
            /// <summary>
            /// Присвоение значения
            /// </summary>
            assign,
            /// <summary>
            /// Значение
            /// </summary>
            value,
            /// <summary>
            /// Пустота
            /// </summary>
            None
        }
    }
}
