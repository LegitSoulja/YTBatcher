using System;

namespace YTBatcher
{
    internal static class Logger
    {

        private readonly static ConsoleColor Color = Console.ForegroundColor;
        private readonly static object _lock = new();

        internal static class Write
        {
            internal static void Log(object data, params object[] args)
                => Console.Write(data.ToString(), args);

            internal static void Acknowledge(object data, params object[] args)
            {
                lock (_lock)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Log(data, args);
                    Console.ForegroundColor = Color;
                }
            }

            internal static void Warn(object data, params object[] args)
            {
                lock (_lock)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Log(data, args);
                    Console.ForegroundColor = Color;
                }
            }

            internal static void Error(object data, params object[] args)
            {
                lock (_lock)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Log(data, args);
                    Console.ForegroundColor = Color;
                }
            }

            internal static void Success(object data, params object[] args)
            {
                lock (_lock)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Log(data, args);
                    Console.ForegroundColor = Color;
                }
            }

            internal static void LogAndTitle(object data, params object[] args)
            {
                Log(data.ToString(), args);
                Title(data.ToString(), args);
            }

            internal static void WarnAndTitle(object data, params object[] args)
            {
                Warn(data.ToString(), args);
                Title(data.ToString(), args);
            }

            internal static void ErrorAndTitle(object data, params object[] args)
            {
                Error(data.ToString(), args);
                Title(data.ToString(), args);
            }

            internal static void SuccessAndTitle(object data, params object[] args)
            {
                Success(data.ToString(), args);
                Title(data.ToString(), args);
            }

            internal static void AcknowledgeAndTitle(object data, params object[] args)
            {
                Acknowledge(data.ToString(), args);
                Title(data.ToString(), args);
            }
        }

        internal static void Log(object data, params object[] args)
    => Console.WriteLine(data.ToString(), args);

        internal static void Acknowledge(object data, params object[] args)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Log(data, args);
                Console.ForegroundColor = Color;
            }
        }

        internal static void Warn(object data, params object[] args)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Log(data, args);
                Console.ForegroundColor = Color;
            }
        }

        internal static void Error(object data, params object[] args)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log(data, args);
                Console.ForegroundColor = Color;
            }
        }

        internal static void Success(object data, params object[] args)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Log(data, args);
                Console.ForegroundColor = Color;
            }
        }

        internal static void LogAndTitle(object data, params object[] args)
        {
            Log(data.ToString(), args);
            Title(data.ToString(), args);
        }

        internal static void WarnAndTitle(object data, params object[] args)
        {
            Warn(data.ToString(), args);
            Title(data.ToString(), args);
        }

        internal static void ErrorAndTitle(object data, params object[] args)
        {
            Error(data.ToString(), args);
            Title(data.ToString(), args);
        }

        internal static void SuccessAndTitle(object data, params object[] args)
        {
            Success(data.ToString(), args);
            Title(data.ToString(), args);
        }

        internal static void AcknowledgeAndTitle(object data, params object[] args)
        {
            Acknowledge(data.ToString(), args);
            Title(data.ToString(), args);
        }

        internal static void Title(object data, params object[] args)
            => Console.Title = string.Format(data.ToString(), args);
    }
}
