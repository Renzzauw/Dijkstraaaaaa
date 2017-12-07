using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;


namespace Concurrency_P2
{
    class Program
    {
        public static Dictionary<int, Connection> Buren = new Dictionary<int, Connection>();
        public static ushort poortNummer;

        static void Main(string[] args)
        {
            // Input
            string firstLine = Console.ReadLine();
            string[] input = firstLine.Split();
            poortNummer = ushort.Parse(input[0]);
            //ushort[] buurPoorten = new ushort[input.Length -1];
            for (int i = 1; i < input.Length; i++)
            {
                Buren.Add(int.Parse(input[i]), new Connection(int.Parse(input[i])));
            }
        }
    }
}
