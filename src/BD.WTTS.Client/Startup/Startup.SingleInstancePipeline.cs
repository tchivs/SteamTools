// ReSharper disable once CheckNamespace
namespace BD.WTTS;

partial class Startup // 本应用程序单例管道
{
    SingleInstancePipeline? singleInstancePipeline;

    bool InitSingleInstancePipeline(Func<string>? sendMessage = null)
    {
        var isInitSingleInstancePipelineReset = false;
    initSingleInstancePipeline: singleInstancePipeline = new();
        if (!singleInstancePipeline.IsFirstSelfApp)
        {
            if (SingleInstancePipeline.SendMessage(sendMessage?.Invoke() ?? ""))
            {
                return false;
            }
            else
            {
                if (!isInitSingleInstancePipelineReset &&
                    SingleInstancePipeline.TryKillCurrentAllProcess())
                {
                    isInitSingleInstancePipelineReset = true;
                    singleInstancePipeline.Dispose();
                    goto initSingleInstancePipeline;
                }
                else
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// 本应用程序单例管道
    /// </summary>
    sealed class SingleInstancePipeline : IDisposable
    {
        bool disposedValue;

        /// <summary>
        /// 是否是第一个启动的本应用程序
        /// </summary>
        public bool IsFirstSelfApp { get; }

        /// <summary>
        /// 在接收新启动的实例的消息时发生
        /// </summary>
        public event Action<string>? MessageReceived;

        public SingleInstancePipeline()
        {
            IsFirstSelfApp = GetIsFirstSelfApp();
            if (IsFirstSelfApp)
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    RunSingleInstancePipeServer();
                });
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool GetIsFirstSelfApp()
        {
            var query = GetCurrentAllProcess();
            var result = query.Any();
            return !result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static IEnumerable<Process> GetCurrentAllProcess()
        {
            var current = Process.GetCurrentProcess();
#if DEBUG
            if (string.Equals("dotnet", Path.GetFileNameWithoutExtension(current.MainModule?.FileName), StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<Process>();
            }
#endif
            var currentMainModule = current.TryGetMainModule();
            var query = from p in Process.GetProcessesByName(current.ProcessName)
                        let pMainModule = p.TryGetMainModule()
                        where p.Id != current.Id &&
                            p.ProcessName == current.ProcessName &&
                            (currentMainModule == null ||
                                (pMainModule != null &&
                                    pMainModule.FileName == currentMainModule.FileName &&
                                        pMainModule.ModuleName == currentMainModule.ModuleName))
                        select p;
            return query;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryKillCurrentAllProcess()
        {
            foreach (var p in GetCurrentAllProcess())
            {
                try
                {
#if NETSTANDARD
                    p.Kill();
#else
                    p.Kill(true);
#endif
                    p.WaitForExit(TimeSpan.FromSeconds(15));
                }
                catch
                {
                }
            }
            return GetIsFirstSelfApp();
        }

        #region Pipes

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string GetPipeName()
        {
            var processPath = Environment.ProcessPath ?? string.Empty;
            return AssemblyInfo.APPLICATION_ID + "_v3_" + Hashs.String.Crc32(processPath);
        }

        CancellationTokenSource? cts;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RunSingleInstancePipeServer()
        {
            cts = new CancellationTokenSource();
            var name = GetPipeName();
            while (true)
            {
                using var pipeServer = new NamedPipeServerStream(name, PipeDirection.In, 1);
                try
                {
                    pipeServer.WaitForConnection();
                    using var sr = new StreamReader(pipeServer);
                    //int i = 0;
                    try
                    {
                        var line = sr.ReadLine();
                        //Console.WriteLine($"({i++})RunPipeServer line: {line ?? "null"}, name: {name}");
                        if (line != null)
                        {
                            MessageReceived?.Invoke(line);
                        }
                    }
                    catch (IOException)
                    {
                    }
                    pipeServer.Close();
                    cts.Token.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    GlobalExceptionHandler.Handler(ex, nameof(RunSingleInstancePipeServer));
                    return;
                }
            }
        }

        /// <summary>
        /// 给主进程发送消息
        /// </summary>
        /// <param name="value"></param>
        public static bool SendMessage(string value)
        {
            var name = GetPipeName();
            try
            {
                using var pipeClient = new NamedPipeClientStream(".", name,
                    PipeDirection.Out, PipeOptions.None,
                    TokenImpersonationLevel.Impersonation);
                pipeClient.Connect(TimeSpan.FromSeconds(25));
                using StreamWriter sw = new StreamWriter(pipeClient);
                sw.WriteLine(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    cts?.Cancel();
                    MessageReceived = null;
                }

                // 释放未托管的资源(未托管的对象)并替代终结器
                // 将大型字段设置为 null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}