using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrency_P2
{
    class Program
    {
        static void Main(string[] args)
        {
            // Input
            string firstLine = Console.ReadLine();
            string[] input = firstLine.Split();
            Console.WriteLine(input[0]);
            ushort poortNummer = ushort.Parse(input[0]);
            ushort[] buurPoorten = new ushort[input.Length -1];
            for (int i = 1; i < input.Length; i++)
            {
                buurPoorten[i - 1] = ushort.Parse(input[i]);
            }
        }
    }
}
