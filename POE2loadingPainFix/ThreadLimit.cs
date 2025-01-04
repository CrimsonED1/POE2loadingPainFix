using System.Diagnostics;
using System.Runtime.InteropServices;

namespace POE2loadingPainFix
{
    /// <summary>
    /// this almost Co-Pilot generated XD
    /// </summary>
    public static class ThreadLimit
    {

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(int dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        private static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        private const int THREAD_SUSPEND_RESUME = 0x0002;


        public static void ThrottleProcess(Process process)
        {
            // Replace with the target process ID and thread ID
            int targetProcessId = process.Id;

            

            Process targetProcess = Process.GetProcessById(targetProcessId);
            var threads = new List<ProcessThread>();
            foreach (ProcessThread thread in targetProcess.Threads)
                threads.Add(thread);
            int thread_count = threads.Count;
            int thread_done_count = 0;

            var sw  = Stopwatch.StartNew();
            foreach (ProcessThread thread in threads)
            {
                string step = "";
                int thread_id = thread.Id;
                try
                {
                    step = "Pre Open";
                    IntPtr hThread = OpenThread(THREAD_SUSPEND_RESUME, false, (uint)thread_id);

                    if (hThread != IntPtr.Zero)
                    {
                        step = "Pre SuspendThread";
                        SuspendThread(hThread);
                        Thread.Sleep(100);
                        // Perform your operations here
                        step = "Pre ResumeThread";
                        ResumeThread(hThread);
                        step = "Pre CloseHandle";
                        CloseHandle(hThread);
                        thread_done_count++;
                    }
                    break;
                }
                catch (Exception ex)
                {
#if DEBUG
                    Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - ThrottleProcess THREAD: {thread_id} STEP: {step} Exception: {ex.Message}");
#endif
                }
            }

            sw.Stop();
#if DEBUG
            Trace.WriteLine($"{DateTime.Now.ToFullDT_German()} - ThrottleProcess Threads: {thread_done_count}/{thread_count} Time: {sw.Elapsed.TotalMilliseconds:N1} msecs");
#endif

        }//for

    }//func
}

