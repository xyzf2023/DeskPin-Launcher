using System.IO;
using System.IO.Pipes;
using System.Windows;

namespace FloatingShortcut.Services;

/// <summary>
/// 单实例 Mutex + 命名管道：二次启动时通知主实例新建空白窗口。
/// </summary>
public sealed class SingleInstanceService : IDisposable
{
    public const string MutexName = "DeskPinLauncher_FloatingShortcut_SingleInstance";
    public const string PipeName = "DeskPinLauncher_FloatingShortcut_Pipe";
    public const string MessageNewBlankWindow = "NEW_BLANK_WINDOW";

    private readonly Mutex _mutex;
    private CancellationTokenSource? _cts;
    private Task? _pipeLoopTask;
    private bool _stopped;

    private SingleInstanceService(Mutex mutex)
    {
        _mutex = mutex;
    }

    /// <summary>若成功取得主实例 Mutex，返回服务实例；否则返回 null（表示已有实例在运行）。</summary>
    public static SingleInstanceService? TryAcquirePrimary()
    {
        try
        {
            var createdNew = false;
            var mutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew)
            {
                mutex.Dispose();
                return null;
            }

            return new SingleInstanceService(mutex);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>向主实例发送“新建空白窗口”消息。失败返回 false。</summary>
    public static bool TryNotifyNewBlankWindow()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(3000);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine(MessageNewBlankWindow);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>在主线程外阻塞等待管道连接，收到消息后切回 UI 线程执行回调。</summary>
    public void StartPipeServer(ShortcutWindowManager manager)
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _pipeLoopTask = RunPipeServerLoop(manager, token);
    }

    private static async Task RunPipeServerLoop(ShortcutWindowManager manager, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(ct).ConfigureAwait(false);

                string? line;
                using (var reader = new StreamReader(server))
                {
                    line = await reader.ReadLineAsync().ConfigureAwait(false);
                }

                if (line != MessageNewBlankWindow)
                {
                    continue;
                }

                var app = System.Windows.Application.Current;
                if (app?.Dispatcher is null)
                {
                    continue;
                }

                await app.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        manager.NewBlankWindowFromExternalLaunch();
                    }
                    catch
                    {
                        // 忽略 UI 侧异常，避免管道线程崩溃
                    }
                });
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch
            {
                // 单次连接失败不影响后续监听
            }
        }
    }

    public void Stop()
    {
        if (_stopped)
        {
            return;
        }

        try
        {
            try
            {
                _cts?.Cancel();
            }
            catch
            {
                // ignore
            }

            try
            {
                if (_pipeLoopTask is not null)
                {
                    _pipeLoopTask.Wait(TimeSpan.FromSeconds(3));
                }
            }
            catch
            {
                // ignore
            }

            _pipeLoopTask = null;
            _cts?.Dispose();
            _cts = null;

            try
            {
                _mutex.ReleaseMutex();
            }
            catch
            {
                // ignore
            }

            try
            {
                _mutex.Dispose();
            }
            catch
            {
                // ignore
            }
        }
        finally
        {
            _stopped = true;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
