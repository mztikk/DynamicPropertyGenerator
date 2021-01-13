using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicPropertyTests
{
    [TestClass]
    public class SetTests
    {
        [DataTestMethod]
        [DataRow("test")]
        [DataRow("1234")]
        [DataRow("test1234")]
        [DataRow("üäöß!ß'\"}{]12391784´°!§&%")]
        public void SetString(string value)
        {
            var testClass = new DynamicTestClass();

            DynamicProperty.Set(testClass, nameof(DynamicTestClass.StringProperty), value);

            Assert.AreEqual(value, testClass.StringProperty);
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(1234)]
        [DataRow(int.MaxValue)]
        [DataRow(int.MinValue)]
        public void SetInt(int value)
        {
            var testClass = new DynamicTestClass();

            DynamicProperty.Set(testClass, nameof(DynamicTestClass.IntProperty), value.ToString());

            Assert.AreEqual(value, testClass.IntProperty);
        }

        [DataTestMethod]
        [DataRow("max", "mustermann")]
        public void SetPerson(string name, string lastname)
        {
            var testClass = new DynamicTestClass();

            DynamicProperty.Set(testClass, nameof(DynamicTestClass.PersonProperty), new Person { Name = name, LastName = lastname }.ToString());

            Assert.AreEqual(new Person { Name = name, LastName = lastname }, testClass.PersonProperty);
        }

        [DataTestMethod]
        [DataRow("max", "mustermann")]
        public void SetPersonObject(string name, string lastname)
        {
            var testClass = new DynamicTestClass();

            DynamicProperty.Set(testClass, nameof(DynamicTestClass.PersonProperty), new Person { Name = name, LastName = lastname });

            Assert.AreEqual(new Person { Name = name, LastName = lastname }, testClass.PersonProperty);
        }
    }
}
