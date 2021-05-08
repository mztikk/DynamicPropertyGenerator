using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicPropertyTests
{
    [TestClass]
    public class GetTests
    {
        private readonly Random _random;

        public GetTests() => _random = new Random();

        [DataTestMethod]
        [DataRow("test")]
        [DataRow("1234")]
        [DataRow("test1234")]
        [DataRow("üäöß!ß'\"}{]12391784´°!§&%")]
        public void GetString(string value)
        {
            var testClass = new DynamicTestClass
            {
                StringProperty = value
            };

            Assert.AreEqual(value, DynamicProperty.Get(testClass, nameof(DynamicTestClass.StringProperty)));
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(1234)]
        [DataRow(int.MaxValue)]
        [DataRow(int.MinValue)]
        public void GetInt(int value)
        {
            var testClass = new DynamicTestClass
            {
                IntProperty = value
            };

            Assert.AreEqual(value, testClass.Get(nameof(DynamicTestClass.IntProperty)));
        }

        [TestMethod]
        public void GetRandomInt()
        {
            const int iterations = 10000;
            for (int i = 0; i < iterations; i++)
            {
                int rnd = _random.Next();
                var testClass = new DynamicTestClass { IntProperty = rnd };
                Assert.AreEqual(rnd, testClass.Get(nameof(DynamicTestClass.IntProperty)));
            }
        }

        [DataTestMethod]
        [DataRow("max", "mustermann")]
        public void GetPerson(string name, string lastname)
        {
            var testClass = new DynamicTestClass
            {
                PersonProperty = new Person { Name = name, LastName = lastname }
            };

            Assert.AreEqual(new Person { Name = name, LastName = lastname }, DynamicProperty.Get(testClass, nameof(DynamicTestClass.PersonProperty)));
        }

        [DataTestMethod]
        [DataRow("hans")]
        public void PropertyPath(string name)
        {
            var testClass = new DynamicTestClass
            {
                PersonProperty = new Person { Name = name }
            };

            Assert.AreEqual(name, DynamicProperty.Get(testClass, new Stack<string>(new string[] { "Name", "PersonProperty" })));
        }
    }
}
