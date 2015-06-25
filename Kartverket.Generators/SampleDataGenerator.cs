using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kartverket.Generators
{
    public class SampleDataGenerator
    {

        public static bool SupportsType(string type)
        {
            return GenerateForType(type) != null;
        }

        public static object GenerateForType(string type)
        {
            switch (type)
            {
                case "string": return GenerateString();
                case "integer": return GenerateInteger();
                case "double": return GenerateDouble();
                case "dateTime": return GenerateDateTime();
                default: return null;
            }
        }

        public static string GenerateString()
        {
            return "lorem ipsum";
        }

        public static int GenerateInteger()
        {
            return new Random().Next();
        }

        private static double GenerateDouble()
        {
            return new Random().NextDouble();
        }

        private static DateTime GenerateDateTime()
        {
            return DateTime.Now.ToLocalTime();
        }
    }
}