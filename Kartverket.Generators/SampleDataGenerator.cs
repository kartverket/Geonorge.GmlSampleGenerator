using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kartverket.Generators
{
    public class SampleDataGenerator
    {

        public virtual bool SupportsType(string type)
        {
            return GenerateForType(type) != null;
        }

        public virtual object GenerateForType(string type)
        {
            switch (type)
            {
                case "string": return GenerateString();
                case "integer": return GenerateInteger();
                case "double": return GenerateDouble();
                case "date": return GenerateDate();
                case "dateTime": return GenerateDateTime();
                case "boolean": return GenerateBoolean();

                default: return null;
            }
        }

        private object GenerateDate()
        {
           return DateTime.Now.ToString("yyyy-MM-dd");
        }

        private object GenerateBoolean()
        {
            return new Random().Next(100) > 50 ? true : false;
        }

        public static string GenerateString()
        {
            return "Lorem ipsum";
        }

        public static int GenerateInteger()
        {
            return new Random().Next();
        }

        public static double GenerateDouble()
        {
            return new Random().NextDouble();
        }

        public static DateTime GenerateDateTime()
        {
            return DateTime.Now.ToLocalTime();
        }
         
    }
}