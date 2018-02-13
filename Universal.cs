using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Block2Json
{
    static public class Universal
    {
        static public bool Match(string pool, string match)
        {
            if ((Regex.Match(match, @"^\d+$").Success || Regex.Match(pool, @"^\d+$").Success) && pool != "" && pool != "x" && match != "" && match != "x")
            {
                if (pool == match) return true;
                else return false;
            }
            else if (match != null && Regex.Match(match, @"\S+").Success && pool != "" && pool != "x" && match != "" && match != "x")
            {
                match = match.Replace(":", "=");
                pool = pool.Replace(":", "=");

                var d = match.Split(',');
                if (d.Length == 0) return true;

                var success = true;
                foreach (var c in d)
                {
                    if (!(pool.Contains(c))) success = false;
                }
                return success;
            }
            else
                return true;
        }
        static public bool Match(List<string> pool, List<string> match)
        {
            if (match != null && match.Count > 0)
            {
                var success = true;
                foreach (var c in match)
                {
                    if (!(pool.Contains(c))) success = false;
                }
                return success;
            }
            else return true;
        }
        static public bool MatchBlock(BlockInfo blockInfo, SimpleBlockInfo simpleBlockInfo)
        {
            return (Match(blockInfo.Data, simpleBlockInfo.Data)) && blockInfo.Id == simpleBlockInfo.Id;
        }
    }
    public class Vector3
    {
        private double _x = 0;
        public double X { get { return _x; } set { if (value != 0) _x = value; } } //x
        private double _y = 0;
        public double Y { get { return _y; } set { if (value != 0) _y = value; } } //y
        private double _z = 0;
        public double Z { get { return _z; } set { if (value != 0) _z = value; } } //z
    }

    static class Current
    {
        //Console Read % Write
        static public int CurLine = 0;
        static public void WriteLine(string text)
        {
            Console.BackgroundColor = Back;
            Console.ForegroundColor = Fore;

            Console.CursorTop = CurLine;
            Console.WriteLine(text);
            CurLine++;
        }
        static public void Write(string text)
        {
            Console.CursorTop = CurLine;
            Console.CursorLeft = 0;
            Console.Write(text);
        }
        static public string ReadLine()
        {
            CurLine++;
            return Console.ReadLine();
        }
        static public void Error(string error, bool display = false, bool Exit = false, Exception ex = null)
        {
            if (display)
            {
                Console.CursorTop = CurLine;
                Console.CursorLeft = 0;
                WriteLine("- Error: " + error);

                if (ex != null) WriteLine("-- " + ex.ToString());

                if (!Program.nopause)
                {
                    WriteLine("[ Press any key to continue... ]");
                    Console.ReadKey(true);
                }
            }
            else
            {
                AddLog("- Error: " + error);
            }

            if (Exit)
            {
                if (Program.log)
                    File.WriteAllText("Log -" + DateTime.Now.ToFileTime() + ".txt", Log.ToString());
                Environment.Exit(1);
            }
        }
        //Progress Bar
        static private int BarLine = 0;
        static private ConsoleColor Back = Console.BackgroundColor;
        static private ConsoleColor Fore = Console.ForegroundColor;
        static public void SetProgressBar()
        {
            var Back = Console.BackgroundColor;

            BarLine = Console.CursorTop;
            CurLine = BarLine;
            Console.BackgroundColor = ConsoleColor.Gray;
            for (int i = 1; i <= 32; i++)
            {
                Console.CursorLeft = i;
                Console.Write(" ");
            }

            Console.BackgroundColor = Back;

            CurLine++;
        }
        static public void DrawProgressBar(int per)
        {
            var Back = Console.BackgroundColor;
            var Fore = Console.ForegroundColor;

            Console.CursorTop = BarLine;

            if (Console.CursorLeft == 0) Console.BackgroundColor = Back;
            else Console.BackgroundColor = ConsoleColor.Green;

            for (int i = 1; i <= 32 * per / 100; i++)
            {
                Console.CursorLeft = i;
                Console.Write(" ");
            }
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.SetCursorPosition(34, BarLine);
            Console.Write(" {0}% ", per);

            Console.CursorVisible = false;

            if (per == 100)
            {
                Console.CursorTop = CurLine + 1;
                Console.CursorVisible = true;
            }

            Console.BackgroundColor = Back;
            Console.ForegroundColor = Fore;
        }
        //Log
        static public StringBuilder Log = new StringBuilder();
        static public void AddLog(string text)
        {
            if (Program.log)
                Log.AppendLine(" - [" + DateTime.Now.ToString() + "] " + text);
        }
    }

    public static class StaticRandom
    {
        private static Random _global = new Random();
        private static ThreadLocal<Random> _local = new ThreadLocal<Random>(() =>
        {
            int seed;
            lock (_global) seed = _global.Next();
            return new Random(seed);
        });

        public static double NextDouble()
        {
            return _local.Value.NextDouble();
        }
    }
}
