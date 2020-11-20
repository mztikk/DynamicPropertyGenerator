using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicPropertyTests
{
    [TestClass]
    public class GetTests
    {
        [DataTestMethod]
        [DataRow("test")]
        [DataRow("1234")]
        [DataRow("test1234")]
        [DataRow("����!�'\"}{]12391784��!�&%")]
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

            Assert.AreEqual(value, DynamicProperty.Get(testClass, nameof(DynamicTestClass.IntProperty)));
        }
    }
}