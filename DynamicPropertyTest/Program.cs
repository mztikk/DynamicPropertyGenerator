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
            System.Console.WriteLine(DynamicProperty.Get(testa, "StringA"));
            System.Console.WriteLine(DynamicProperty.Get(testa, "IntA"));
            DynamicProperty.Set(testa, "StringA", "DynamicProperty is awesome!");
            DynamicProperty.Set(testa, "IntA", "50");
            System.Console.WriteLine(DynamicProperty.Get(testa, "StringA"));
            System.Console.WriteLine(DynamicProperty.Get(testa, "IntA"));
        }
    }
}
