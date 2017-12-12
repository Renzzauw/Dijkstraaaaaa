using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

namespace CCPract2
{
    class Program
    {
        public static int myPort;
        public static Dictionary<int, List<Connection>> neighbours = new Dictionary<int, List<Connection>>();

        static void Main(string[] args)
        {
            myPort = int.Parse(args[0]);

            Console.Title = "NetChange " + myPort;

            new Server(myPort);

            for (int i = 1; i < args.Length; i++)
            {
                int port = int.Parse(args[i]);
                AddConnectionToDictionary(port, new Connection(port));
            }
        }

        public static void AddConnectionToDictionary(int port, Connection connection)
        {
            if (!neighbours.ContainsKey(port))
            {
                List<Connection> list = new List<Connection>();
                neighbours.Add(port, list);
            }
            neighbours[port].Add(connection);
        }
    }

    class Server
    {
        public Server(int port)
        {
            // Luister op de opgegeven poort naar verbindingen
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();

            // Start een aparte thread op die verbindingen aanneemt
            new Thread(() => AcceptLoop(server)).Start();
        }

        private void AcceptLoop(TcpListener handle)
        {
            while (true)
            {
                TcpClient client = handle.AcceptTcpClient();
                StreamReader clientIn = new StreamReader(client.GetStream());
                StreamWriter clientOut = new StreamWriter(client.GetStream());
                clientOut.AutoFlush = true;

                // De server weet niet wat de poort is van de client die verbinding maakt, de client geeft dus als onderdeel van het protocol als eerst een bericht met zijn poort
                int hisPort = int.Parse(clientIn.ReadLine().Split()[1]);

                Console.WriteLine("Verbonden: " + hisPort);

                // Zet de nieuwe verbinding in de verbindingslijst
                Program.AddConnectionToDictionary(hisPort, new Connection(clientIn, clientOut));
            }
        }
    }

    class Connection
    {
        public StreamReader Read;
        public StreamWriter Write;

        // Connection heeft 2 constructoren: deze constructor wordt gebruikt als wij CLIENT worden bij een andere SERVER
        public Connection(int port)
        {
            here:
            try
            {
                TcpClient client = new TcpClient("localhost", port);
                Read = new StreamReader(client.GetStream());
                Write = new StreamWriter(client.GetStream());
                Write.AutoFlush = true;
            }
            catch
            {
                Thread.Sleep(5);
                goto here;
            }

            // De server kan niet zien van welke poort wij client zijn, dit moeten we apart laten weten
            Write.WriteLine("Poort: " + Program.myPort);

            // Start het reader-loopje
            new Thread(ReaderThread).Start();
        }

        // Deze constructor wordt gebruikt als wij SERVER zijn en een CLIENT maakt met ons verbinding
        public Connection(StreamReader read, StreamWriter write)
        {
            Read = read; Write = write;

            // Start het reader-loopje
            new Thread(ReaderThread).Start();
        }

        // LET OP: Nadat er verbinding is gelegd, kun je vergeten wie er client/server is (en dat kun je aan het Connection-object dus ook niet zien!)

        // Deze loop leest wat er binnenkomt en print dit
        public void ReaderThread()
        {
            try
            {
                while (true)
                {
                    string[] input = Read.ReadLine().Split();
                    //Console.WriteLine(input);


                    Console.WriteLine("Console");
                    Write.WriteLine("Write");

                    // Gebruiker vraagt om routing table
                    if (input[0] == "R")
                    {
                        // Print routing table

                        int distance = 0;
                        Write.WriteLine(Program.myPort + " " + distance + " local");
                        distance++;
                        foreach (KeyValuePair<int, List<Connection>> kv in Program.neighbours)
                        {
                            Write.WriteLine(kv.Key + " " + distance + " " + kv.Key);
                        }
                    }
                    else if (input[0] == "B")
                    {
                        int port = int.Parse(input[1]);
                        string message = "";
                        for (int i = 2; i < input.Length - 1; i++)
                        {
                            message += input[2] + " ";
                        }
                        message += input[input.Length - 1];

                        // Send Message to Port
                    }
                    else if (input[0] == "C")
                    {
                        int port = int.Parse(input[1]);

                        // Connect to Port
                    }
                    else if (input[0] == "D")
                    {
                        int port = int.Parse(input[1]);

                        // Disconnect from Port
                    }
                }
            }
            catch
            {
                // Verbinding is kennelijk verbroken
                Console.WriteLine("Verbroken: " + Program.myPort);
            }
        }
    }
}