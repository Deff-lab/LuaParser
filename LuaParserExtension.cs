using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaParser
{
    public static class LuaParserExtension
    {
        public static string FromToDivide(string data, char from, char to, out string parts, string replace = "")
        {
            int start = data.IndexOf(from);

            int end = start + 1;

            int counter = 1;

            while (counter != 0)
            {
                int startIn = data.IndexOf(from, end);
                int endIn = data.IndexOf(to, end);

                if (startIn != -1 && startIn < endIn)
                {
                    end = data.IndexOf(from, end) + 1;

                    counter++;
                }
                else
                {
                    end = data.IndexOf(to, end) + 1;
                    counter--;
                }
            }

            parts = data.Substring(start, end - start);
            data = data.Remove(start, end - start).Insert(start, replace);

            return data;
        }

        public static string Divide(string data, char from, char to)
        {
            int start = data.IndexOf(from);

            int end = start + 1;

            int counter = 1;

            while (counter != 0)
            {
                int startIn = data.IndexOf(from, end);
                int endIn = data.IndexOf(to, end);

                if (startIn != -1 && startIn < endIn)
                {
                    end = data.IndexOf(from, end) + 1;

                    counter++;
                }
                else
                {
                    end = data.IndexOf(to, end) + 1;
                    counter--;
                }
            }

            return data.Substring(start, end - start);
        }
    }
}
