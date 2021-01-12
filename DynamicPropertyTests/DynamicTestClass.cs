using System;
using System.Diagnostics.CodeAnalysis;

namespace DynamicPropertyTests
{
    public class DynamicTestClass
    {
        public string StringProperty { get; set; }
        public int IntProperty { get; set; }
        public Person PersonProperty { get; set; }

    }

    public class Person : IEquatable<Person>
    {
        public string Name { get; set; }
        public string LastName { get; set; }

        public bool Equals([AllowNull] Person other) => other is { } && other.Name == Name && other.LastName == LastName;

        public override bool Equals(object obj) => obj is Person p && Equals(p);

        public override int GetHashCode() => HashCode.Combine(Name, LastName);

        public override string ToString() => $"{Name} {LastName}";

        public static Person Parse(string value)
        {
            string[] split = value.Split();
            return new Person { Name = split[0], LastName = split[1] };
        }
    }
}
