using System;
using System.Threading;
using System.Threading.Tasks;

namespace main
{
    public static class ExceptionExtensions
    {
        public static void LogExceptions(this Task task)
        {
            task.ContinueWith(t =>
            {
                var aggException = t.Exception.Flatten();
                foreach (var exception in aggException.InnerExceptions)
                    LogException(exception);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private static void LogException(Exception exception)
        {
            Console.WriteLine($"LogException : {exception.Message}");
        }
    }

    public class NullCrasher
    {
        int age = 10;
        public void Print()
        {
            Console.WriteLine($"Print age is {age}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World! Input Anything...");

            Console.WriteLine($"AppDomain: {AppDomain.CurrentDomain.FriendlyName}");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            bool bRun = true;
            bool bThrowExInTask = false;
            bool bThrowExInThread = false;
            
            while(bRun)
            {
                try
                {
                    if (Console.KeyAvailable)
                    {
                        var input = Console.ReadKey();
                        Console.WriteLine();
                        switch (input.Key)
                        {
                            case ConsoleKey.Q:
                                Console.WriteLine($"Quit");
                                bRun = false;
                                break;
                            // Exception in a Thread In the ThreadPool.
                            case ConsoleKey.A:
                                Console.WriteLine($"Task.Run");
                                bThrowExInTask = false;
                                Task.Run(() =>
                                {
                                    Console.WriteLine($"ConsoleKey.A. Id:{Thread.CurrentThread.ManagedThreadId}, AppDomain:{AppDomain.CurrentDomain.FriendlyName}, Pool: {Thread.CurrentThread.IsThreadPoolThread}");
                                    for (int i = 0; i < 100; ++i)
                                    {
                                        Console.WriteLine($"    ThreadPool : {i}");
                                        Thread.Sleep(100);
                                        if (bThrowExInTask)
                                            throw new Exception($"Task.Run, ThreadPool Exception - {i}");
                                    }
                                });
                                break;
                            case ConsoleKey.S:
                                Console.WriteLine($"Throw Exception in ConsoleKey.A");
                                bThrowExInTask = true;
                                break;
                                // Exception in a Thread Not In the ThreadPool.
                            case ConsoleKey.Z:
                                {
                                    Console.WriteLine($"Thread.Start");
                                    bThrowExInThread = false;
                                    var thread = new Thread(() =>
                                    {
                                        Console.WriteLine($"ConsoleKey.A. Id:{Thread.CurrentThread.ManagedThreadId}, AppDomain:{AppDomain.CurrentDomain.FriendlyName}, Pool: {Thread.CurrentThread.IsThreadPoolThread}");
                                        for (int i = 0; i < 100; ++i)
                                        {
                                            Console.WriteLine($"    NewThread : {i}");
                                            Thread.Sleep(100);
                                            if (bThrowExInThread)
                                                throw new Exception($"Thread.Start, Thread Exception - {i}");
                                        }
                                    });
                                    thread.Start();
                                    break;
                                }
                            case ConsoleKey.X:
                                {
                                    Console.WriteLine($"Throw Exception in ConsoleKey.Z");
                                    bThrowExInThread = true;
                                    break;
                                }
                                // Exception in MainThread
                            case ConsoleKey.E:
                                throw new Exception($"ConsoleKey.E from MainThread");
                            case ConsoleKey.G:
                                Console.WriteLine($"GC.Collect()");
                                GC.Collect();
                                break;
                            case ConsoleKey.R:
                                Console.WriteLine($"GC.Collect()");
                                Console.WriteLine($"GC.WaitForPendingFinalizers()");
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                break;
                                // Null Exception in MainThread
                            case ConsoleKey.N:
                                {
                                    Console.WriteLine($"Make Null Exception");
                                    NullCrasher a = new NullCrasher();
                                    a.Print();
                                    a = null;

                                    a.Print();
                                    break;
                                }
                                // Null Exception using Task.Run
                            case ConsoleKey.M:
                                {
                                    Console.WriteLine($"Make Null Exception In Task.Run");
                                    Task.Run(() =>
                                    {
                                        NullCrasher a = new NullCrasher();
                                        a.Print();
                                        a = null;

                                        a.Print();
                                    });
                                    break;
                                }
                            case ConsoleKey.B:
                                {
                                    Console.WriteLine($"Make Null Exception In Thread.Start");
                                    new Thread(() =>
                                    {
                                        NullCrasher a = new NullCrasher();
                                        a.Print();
                                        a = null;

                                        a.Print();
                                    }).Start();
                                    break;
                                }
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine($"[] WhileLoop Catch Exception : {e.Message}");
                }
            }
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            // task.wait()를 호출하거나, result값에 호출하면서 GC가 task를 수거해갈때, finalize호출될 때 이곳으로 오게 된다.
            // 위 조건에 맞지 않게 그냥 Task.Run()으로 던져버린 작업은 캐치가 불가능한 것 같다.
            Console.WriteLine($"[T] taskscheduler.unobservedtaskexception sender: {sender.ToString()}, e: {e.Exception.Message}");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"[G] appdomain.unhandledexception sender: {sender.ToString()}, e: {e.ExceptionObject}");
        }
    }
}
