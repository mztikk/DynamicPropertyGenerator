namespace DynamicPropertyTest
{
    public record TestRecordA()
    {
        public string StringA { get; set; }
        public string StringB { get; set; }
        public int IntA { get; set; }
        public int IntB { get; set; }
    }
    public record TestRecordB()
    {
        public string StringB { get; set; }
        public int IntB { get; set; }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            //HelloWorldGenerated.HelloWorldClass.HelloWorld();
            var testa = new TestA { StringA = "This is StringA!", IntA = 20 };
            System.Console.WriteLine(testa.DynamicGet("stringa", true));
            System.Console.WriteLine(testa.DynamicGet("IntA"));
            testa.DynamicSet("StringA", "DynamicProperty is awesome!");
            testa.DynamicSet("IntA", "50");
            System.Console.WriteLine(testa.DynamicGet("StringA"));
            System.Console.WriteLine(testa.DynamicGet("IntA"));
        }
    }
}
