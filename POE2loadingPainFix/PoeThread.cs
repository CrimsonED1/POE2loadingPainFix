using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace POE2loadingPainFix
{

    public class PoeThread
    {
        protected object SyncRoot = new object();
        protected Thread? Thread;

        protected LimitMode UsedLimitMode = LimitMode.Off;
        protected Config UsedConfig { get; private set; }

        protected TargetProcess UsedTargetProcess { get; private set; }
        protected bool Terminate { get; private set; } = true;
        protected readonly DateTime StartTime = DateTime.Now;
        protected TimeSpan? CycleTime { get; private set; }

        protected int ThreadSleep_LimitOn { get; set; } = 10;
        protected int ThreadSleep_LimitOff { get; set; } = 100;

        protected DateTime DT_Cylcle { get; private set; } = DateTime.Now;
        protected DateTime DT_LastCylce { get; private set; } = DateTime.Now.AddMinutes(-1);

        public event EventHandler<Exception>? OnThreadException;

        public PoeThread()
        {
            UsedConfig = PoeThreadSharedContext.Instance.Config;
        }

          private void Intrernal_Thread_Execute(object? sender)
        {
            var sw = new Stopwatch();
            while (!Terminate)
            {
                sw.Restart();
                lock (SyncRoot)
                {
                    UsedConfig = PoeThreadSharedContext.Instance.Config;
                    UsedLimitMode = PoeThreadSharedContext.Instance.LimitMode;

                }

                try
                {
                    DT_Cylcle = DateTime.Now;
                    Thread_Execute();
                    DT_LastCylce = DT_Cylcle;
                }
                catch (Exception ex)
                {
                    OnThreadException?.Invoke(this, ex);
                }
                finally
                {
                    sw.Stop();
                    CycleTime = sw.Elapsed;
#if DEBUG
                    if (CycleTime > TimeSpan.FromMilliseconds(150))
                        Trace.WriteLine($"CycleTime High! {CycleTime.Value.TotalMilliseconds}");
#endif
                }




                ////Slow when POE2 started, fast when its not!
                //if (_TP != null)
                //    Thread.Sleep(100);
                //else
                //    Thread.Sleep(1);

                if(UsedLimitMode==LimitMode.On)
                    Thread.Sleep(ThreadSleep_LimitOn);
                else 
                    Thread.Sleep(ThreadSleep_LimitOff);

            }//while

            Debugging.Step();
        }

        protected virtual void Thread_Execute()
        {
            throw new NotImplementedException("must be overriden!");
        }

        public virtual void Start()
        {
            Stop();
 

            if (Thread != null)
            {
                Thread.Join();
                Thread = null;
            }

            Terminate = false;
            Thread = new Thread(new ParameterizedThreadStart(Intrernal_Thread_Execute));
            Thread.IsBackground = true;
            Thread.Priority = ThreadPriority.AboveNormal;
            Thread.Start();

        }

        public virtual void Stop()
        {
            Terminate = true;
            if (Thread != null)
            {
                Thread = null;                
            }
        }
    }
}

