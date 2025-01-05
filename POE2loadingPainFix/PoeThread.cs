using System.Diagnostics;

namespace POE2loadingPainFix
{

    public class PoeThread
    {
        protected object SyncTransferState = new object();
        private Thread? Thread;

        public virtual string Caption => "";




        protected ThreadPriority ThreadPriority = ThreadPriority.AboveNormal;

        protected LimitMode CurrentLimitMode = LimitMode.Off;
        protected Config UsedConfig { get; private set; }

        protected TargetProcess? UsedTP { get; set; }
        protected bool Terminate { get; private set; } = true;
        protected readonly DateTime StartTime = DateTime.Now;

        protected int Next_ThreadSleep_LimitOn { get; set; } = 10;
        protected int Next_ThreadSleep_LimitOff { get; set; } = 100;

        public DateTime DT_Cylcle { get; set; } = DateTime.Now;
        public DateTime DT_LastCylce { get; set; } = DateTime.Now.AddMinutes(-1);
        protected ThreadState ThreadState;
        protected ThreadState ThreadStateReady;

        protected PoeThread()
        {
            UsedConfig = PoeThreadSharedContext.Instance.Config;
            ThreadState = new ThreadState(this.GetType(), this.Caption);
            ThreadStateReady = new ThreadState(this.GetType(), this.Caption);
        }




        /// <summary>
        /// take state and generate new!
        /// </summary>
        /// <returns></returns>
        public ThreadState TakeThreadState()
        {
            ThreadState res;
#if DEBUG
            var sw = Stopwatch.StartNew();
#endif

            lock (SyncTransferState)
            {
                var current = ThreadStateReady;
                ThreadStateReady = new ThreadState(this.GetType(), this.Caption);
                res = current;
            }
#if DEBUG
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 1)
                Debugging.Step();
#endif

            return res;
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


                try
                {
                        DT_Cylcle = DateTime.Now;
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
                        DT_LastCylce = DT_Cylcle;
                        ThreadState.Exception = null;
                }
                catch (Exception ex)
                {
                    ThreadState.Exception = ex;
                }
                finally
                {
                    sw.Stop();
                    ThreadState.CycleTime = sw.Elapsed;
#if DEBUG
                    if (ThreadState.CycleTime > TimeSpan.FromMilliseconds(500))
                        Trace.WriteLine($"CycleTime High! Thread: {ThreadState.ThreadType.Name} : {ThreadState.CycleTime.Value.TotalMilliseconds}");
#endif
                }

                try
                {
                    Thread_Execute_2();
                }
                catch
                { }


                lock(SyncTransferState)
                {
                    ThreadStateReady.MoveDataFrom(ThreadState);
                }


                ////Faster when POE2 is limited
                if (CurrentLimitMode == LimitMode.On)
                    Thread.Sleep(Next_ThreadSleep_LimitOn);
                else
                    Thread.Sleep(Next_ThreadSleep_LimitOff);

            }//while

            Debugging.Step();
        }

        protected void AddMeasure(string measureName, double value)
        {
            if (DT_Cylcle == DT_LastCylce)
                return; //prevent adding dupes!

            List<DtValue> values;
            if (!ThreadState.Measures.TryGetValue(measureName, out values))
            {
                values = new List<DtValue>();
                ThreadState.Measures.Add(measureName, values);
            }

            values.Add(new DtValue(DT_Cylcle, value));
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

