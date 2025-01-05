using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace POE2loadingPainFix
{

    public class PoeThread
    {
        protected object SyncState = new object();
        private Thread? Thread;




        

        protected ThreadPriority ThreadPriority = ThreadPriority.AboveNormal;

        protected LimitMode CurrentLimitMode = LimitMode.Off;
        protected Config UsedConfig { get; private set; }

        protected TargetProcess? UsedTP { get; set; }
        protected bool Terminate { get; private set; } = true;
        protected readonly DateTime StartTime = DateTime.Now;

        protected int Next_ThreadSleep_LimitOn { get; set; } = 10;
        protected int Next_ThreadSleep_LimitOff { get; set; } = 100;


        protected PoeThread()
        {
            UsedConfig = PoeThreadSharedContext.Instance.Config;
            ThreadState = new ThreadState(this.GetType());
        }

        public bool IsThreadStateReady { get; set; } = false;

        private ThreadState _ThreadState;
        protected ThreadState ThreadState
        {
            get
            {
                lock (SyncState)
                {
                    return _ThreadState;
                }
            }
            set
            {
                lock (SyncState)
                {
                    _ThreadState = value;
                }
            }
        }

        /// <summary>
        /// take state and generate new!
        /// </summary>
        /// <returns></returns>
        public ThreadState TakeThreadState()
        {
            lock (SyncState)
            {
                var current = ThreadState;
                ThreadState = new ThreadState(this.GetType());
                return current;
            }
        }

        private void Intrernal_Thread_Execute(object? sender)
        {
            var sw = new Stopwatch();
            while (!Terminate)
            {
                sw.Restart();
                UsedConfig = PoeThreadSharedContext.Instance.Config;
                UsedTP = PoeThreadSharedContext.Instance.TargetProcess;

                Next_ThreadSleep_LimitOn = 10;
                Next_ThreadSleep_LimitOff = 100;

                if (UsedTP == null)
                    IsThreadStateReady = false;

                lock (SyncState) //lock until cycle done
                {

                    try
                    {
                        ThreadState.DT_Cylcle = DateTime.Now;
                        Process? process = null;
                        if (UsedTP != null)
                        {
                            try
                            {
                                process = Process.GetProcessById(UsedTP.PID);
                                if (process.HasExited)
                                {
                                    process = null;
                                    UsedTP = null;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debugging.Step();
                                UsedTP = null;
                            }
                        }


                        Thread_Execute(process);
                        ThreadState.DT_LastCylce = ThreadState.DT_Cylcle;
                        ThreadState.Exception = null;
                        if(UsedTP!=null)
                            IsThreadStateReady = true;
                    }
                    catch (Exception ex)
                    {
                        ThreadState.Exception = ex;
                        IsThreadStateReady = true;
                    }
                    finally
                    {
                        sw.Stop();
                        ThreadState.CycleTime = sw.Elapsed;
#if DEBUG
                        if (ThreadState.CycleTime > TimeSpan.FromMilliseconds(150))
                            Trace.WriteLine($"CycleTime High! Thread: {ThreadState.ThreadType.Name} : {ThreadState.CycleTime.Value.TotalMilliseconds}");
#endif
                    }


                    try
                    {
                        Thread_Execute_2();
                    }
                    catch
                    { }
                }//lock syncstate

                ////Faster when POE2 is limited
                if (CurrentLimitMode==LimitMode.On)
                    Thread.Sleep(Next_ThreadSleep_LimitOn);
                else 
                    Thread.Sleep(Next_ThreadSleep_LimitOff);

            }//while

            Debugging.Step();
        }

        protected virtual void Thread_Execute(Process? poeProcess)
        {
            throw new NotImplementedException("must be overriden!");
        }

        protected virtual void Thread_Execute_2()
        {
            //must not be overriden...
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
            Thread.Priority = ThreadPriority;
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

