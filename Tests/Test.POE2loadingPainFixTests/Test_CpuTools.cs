using System.Linq;

namespace Test.POE2loadingPainFixTests
{
    [TestClass]
    public sealed class Test_CpuTools
    {
        [TestMethod]
        public void Test_GetProcessor_Count()
        {

            {
                int procs1 = POE2loadingPainFix.CpuTools.GetProcessorsCount_FromAffinity(0xFF);
                Assert.AreEqual<int>(2 * 4, procs1);
            }

            {
                int procs1 = POE2loadingPainFix.CpuTools.GetProcessorsCount_FromAffinity(0x01);
                Assert.AreEqual<int>(1, procs1);
            }
            {
                int procs1 = POE2loadingPainFix.CpuTools.GetProcessorsCount_FromAffinity(0x02);
                Assert.AreEqual<int>(1, procs1);
            }
            {
                int procs1 = POE2loadingPainFix.CpuTools.GetProcessorsCount_FromAffinity(0x03);
                Assert.AreEqual<int>(2, procs1);
            }
            {
                int procs1 = POE2loadingPainFix.CpuTools.GetProcessorsCount_FromAffinity(0x07);
                Assert.AreEqual<int>(3, procs1);
            }
            {
                int procs1 = POE2loadingPainFix.CpuTools.GetProcessorsCount_FromAffinity(0x0F);
                Assert.AreEqual<int>(4, procs1);
            }


            {
                int procs1 = POE2loadingPainFix.CpuTools.GetProcessorsCount_FromAffinity(0xFFFF);
                Assert.AreEqual<int>(4*4, procs1);
            }
            {
                int procs1 = POE2loadingPainFix.CpuTools.GetProcessorsCount_FromAffinity(0x1FF);
                Assert.AreEqual<int>(8+1, procs1);
            }
            {
                int procs1 = POE2loadingPainFix.CpuTools.GetProcessorsCount_FromAffinity(0x9FF);
                Assert.AreEqual<int>(8+2, procs1);
            }
        }

        [TestMethod]
        public void Test_GetProcessorAffinity_Issue_2()
        {

            //https://github.com/CrimsonED1/POE2loadingPainFix/issues/2
            POE2loadingPainFix.CpuTools.Debug_OverrideCPUs = 11;
            {
                var limit = new bool[11];
                limit[0] = true;
                var af1 = POE2loadingPainFix.CpuTools.GetProcessorAffinity(limit);
                Assert.AreEqual<nint>((nint)1, af1);
            }


        }


        public void Test_GetProcessorAffinity(int cpus)
        {
            POE2loadingPainFix.CpuTools.Debug_OverrideCPUs = cpus;

            {
                var limit = new bool[cpus];
                limit[0] = false;
                var af1 = POE2loadingPainFix.CpuTools.GetProcessorAffinity(limit);
                Assert.AreEqual<nint>((nint)1, af1, $"must always return minium 1 (cpus: {cpus}");

            }

            //test 1 true
            {
                var limit = new bool[cpus];
                limit[0] = true;
                var af1 = POE2loadingPainFix.CpuTools.GetProcessorAffinity(limit);
                Assert.AreEqual((nint)1, af1, $"must always return minium 1 (cpus: {cpus}");
            }

            //test 2 true
            {
                var limit = new bool[cpus];
                limit[0] = true;
                limit[1] = true;
                var af1 = POE2loadingPainFix.CpuTools.GetProcessorAffinity(limit);
                Assert.AreEqual((nint)3, af1);
            }

            //test zero range
            {
                var limit = new bool[0];
                var af1 = POE2loadingPainFix.CpuTools.GetProcessorAffinity(limit);
                Assert.AreEqual((nint)1, af1);
            }

            //test more ranges
            {
                var limit = new bool[cpus+10];
                limit[limit.Length - 1] = true;
                var af1 = POE2loadingPainFix.CpuTools.GetProcessorAffinity(limit);
                Assert.AreEqual((nint)1, af1);
            }

        }

        [TestMethod]
        public void Test_GetProcessorAffinity()
        {
            Test_GetProcessorAffinity(2);
            Test_GetProcessorAffinity(4);
            Test_GetProcessorAffinity(11);
            Test_GetProcessorAffinity(15);
            Test_GetProcessorAffinity(16);
            Test_GetProcessorAffinity(32);



        }
    }
}
