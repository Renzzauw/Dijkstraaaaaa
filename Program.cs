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

            Console.Title = "Netchange " + poortNummer;

            new Server(poortNummer);

            //ushort[] buurPoorten = new ushort[input.Length -1];
            for (int i = 1; i < input.Length; i++)
            {
                Buren.Add(int.Parse(input[i]), new Connection(int.Parse(input[i])));
            }

            while (true)
            {
                string[] commando = Console.ReadLine().Split();

                if (commando[0] == "R")
                {
                    // Routing table
                    RoutingTable();
                }
                else if (commando[0] == "B")
                {
                    int portNumber = int.Parse(commando[1]);
                    string message = commando[2];
                    // Send message to portnumber
                }
                else if (commando[0] == "C")
                {
                    int portNumber = int.Parse(commando[1]);
                    // Connect to portnumber
                }
                else if (commando[0] == "D")
                {
                    int portNumber = int.Parse(commando[1]);
                    // Disconnect from portnumber
                }
            }
        }

        static void RoutingTable()
        {
            string result = "";
            foreach (KeyValuePair<int, Connection> kv in Buren)
            {
                int distance = 0;
                result += kv.Key + " " + distance + " " + kv.Value + "\n";
            }
            Console.WriteLine(result);
        }
    }
}
