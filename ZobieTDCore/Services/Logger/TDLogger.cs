using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;
using System.Threading;
using ZobieTDCore.Contracts.Items;
using ZobieTDCore.Contracts;

namespace ZobieTDCore.Services.Logger
{
    public class TDLogger
    {
        private const string PROJECT_TAG = "GameLogger";
        private static readonly IUnityEngineContract unityEngineContract =
            ContractManager.Instance.UnityEngineContract ?? throw new InvalidOperationException("Core engine was not initialized");

        private static StreamWriter LogWriter;
        private static FileStream _logFs;
        private string classTag;
        private static readonly BlockingCollection<string> LogQueue = new BlockingCollection<string>(new ConcurrentQueue<string>());
        private static readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private static string LogFolder;
        private static bool isInitialized = false;

        public static void Init()
        {
            LogFolder = Path.Combine(unityEngineContract.PersistentDataPath, "logs");
            if (!Directory.Exists(LogFolder))
            {
                Directory.CreateDirectory(LogFolder);
            }

            var dateTimeNow = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var logFileName = $"{PROJECT_TAG}_{dateTimeNow}.log";
            var filePath = Path.Combine(LogFolder, logFileName);

            _logFs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
            LogWriter = new StreamWriter(_logFs) { AutoFlush = false };

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Task.Run(() => ProcessLogQueue(Cts.Token));
            isInitialized = true;
        }

        public TDLogger(string tag)
        {
            classTag = tag;
        }

        private static async Task ProcessLogQueue(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (LogQueue.TryTake(out var log, Timeout.Infinite, token))
                    {
                        LogWriter.WriteLine(log);
                        if (LogQueue.Count == 0)
                        {
                            await LogWriter.FlushAsync();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private static void Log(string log)
        {
            unityEngineContract.LogToConsole(log);
            LogQueue.Add(log);
        }

        public void D(string message, [CallerMemberName] string caller = "")
        {
            if (unityEngineContract.IsDevelopmentBuild)
            {
                var log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\tD\t{PROJECT_TAG}\t{classTag}\t{caller}\t{message}";
                Log(log);
            }
        }

        public void I(string message, [CallerMemberName] string caller = "")
        {
            var log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\tI\t{PROJECT_TAG}\t{classTag}\t{caller}\t{message}";
            Log(log);
        }

        public void E(string message, [CallerMemberName] string caller = "")
        {
            var log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\tE\t{PROJECT_TAG}\t{classTag}\t{caller}\t{message}";
            Log(log);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            LogWriter.WriteLine("Unhandled Exception: " + exception?.Message);
            LogWriter.WriteLine("StackTrace: " + exception?.StackTrace);
            LogWriter.WriteLine("Occurred at: " + DateTime.Now);
            LogWriter.Flush();
            LogWriter.Close();
            _logFs.Close();
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            LogWriter.Flush();
            LogWriter.Close();
            _logFs.Close();
        }

        public static class Raw
        {
            public static void D(string message, [CallerMemberName] string caller = "")
            {
                if (unityEngineContract.IsDevelopmentBuild)
                {
                    var log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\tD\t{PROJECT_TAG}\tRaw\t{caller}\t{message}";
                    Log(log);
                }
            }

            public static void I(string message, [CallerMemberName] string caller = "")
            {
                var log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\tI\t{PROJECT_TAG}\tRaw\t{caller}\t{message}";
                Log(log);
            }

            public static void E(string message, [CallerMemberName] string caller = "")
            {
                var log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\tE\t{PROJECT_TAG}\tRaw\t{caller}\t{message}";
                Log(log);
            }
        }
    }

}
