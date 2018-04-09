using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 序列化
{
    class Program
    {
        static void Main(string[] args)
        {
            Test t = new Test() { name = "zhang", age = 9 };
            func(t);
            Console.WriteLine($"{t.name},{t.age}");
            Console.Read();
        }

        static void func(Test t)
        {
            t.name = "wang";
            t.age = 19;
        }

    }
    class Test
    {
        public string name { set; get; }
        public int age { set; get; }
        List<string> Books { set; get; }

    }


    //自动色阶
    class AutoColor
    {
        uint[] setColor(uint[] input)
        {
            uint max = input.Max();
            uint min = input.Min();
            double avreage = getAverage(input);
        }
        double getAverage(uint[] input)
        {
            double res = 0;
            for (int i = 0; i < input.Length; i++)
            {
                res += input[i];
            }
            return res / input.Length;
        }
    }
}
