internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Starting...");
        string pathdummy = @"C:\temp\temp.zip";

        while (true)
        {
            Console.WriteLine("Start Load Data? (Press any key...)");
            Console.ReadKey();

            for(int i = 0; i < 100; i++) 
                {
                    var bytes = System.IO.File.ReadAllBytes(pathdummy);
                    Console.WriteLine($"Load done! Size: {bytes.Length}");
                }

        }
    }
}