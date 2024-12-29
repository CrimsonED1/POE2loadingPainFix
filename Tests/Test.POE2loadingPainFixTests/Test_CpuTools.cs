namespace Test.POE2loadingPainFixTests
{
    [TestClass]
    public sealed class Test_CpuTools
    {
        [TestMethod]
        public void Test_GetProcessorAffinity_Issue_2()
        {

            //https://github.com/CrimsonED1/POE2loadingPainFix/issues/2
            POE2loadingPainFix.CpuTools.Debug_OverrideCPUs = 11;
            {
                var limit = new bool[11];
                limit[0] = true;
                var af1 = POE2loadingPainFix.CpuTools.GetProcessorAffinity(limit);
                Assert.AreEqual<nint>(af1, (nint)1);
            }


        }


        public void Test_GetProcessorAffinity(int cpus)
        {
            POE2loadingPainFix.CpuTools.Debug_OverrideCPUs = cpus;

            {
                var limit = new bool[cpus];
                limit[0] = false;
                var af1 = POE2loadingPainFix.CpuTools.GetProcessorAffinity(limit);
                Assert.AreEqual<nint>(af1, (nint)1, $"must always return minium 1 (cpus: {cpus}");

            }

            //test 1 true
            {
                var limit = new bool[cpus];
                limit[0] = true;
                var af1 = POE2loadingPainFix.CpuTools.GetProcessorAffinity(limit);
                Assert.AreEqual(af1, (nint)1, $"must always return minium 1 (cpus: {cpus}");
            }

            //test 2 true
            {
                var limit = new bool[cpus];
                limit[0] = true;
                limit[1] = true;
                var af1 = POE2loadingPainFix.CpuTools.GetProcessorAffinity(limit);
                Assert.AreEqual(af1, (nint)3);
            }

            //test zero range
            {
                var limit = new bool[0];
                var af1 = POE2loadingPainFix.CpuTools.GetProcessorAffinity(limit);
                Assert.AreEqual(af1, (nint)1);
            }

            //test more ranges
            {
                var limit = new bool[cpus+10];
                limit[limit.Length - 1] = true;
                var af1 = POE2loadingPainFix.CpuTools.GetProcessorAffinity(limit);
                Assert.AreEqual(af1, (nint)1);
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
