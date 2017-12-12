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
        public static Dictionary<int, Connection> neighbours = new Dictionary<int, Connection>();
        public static Dictionary<int, int> preferredNeighbours = new Dictionary<int, int>();
        public static Dictionary<int, int> distanceToPort = new Dictionary<int, int>();

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
                neighbours.Add(port, connection);
            }
        }
    }

    /*class RoutingTable
    {
        public static Dictionary<int, int> portDistances;

        public RoutingTable()
        {
            portDistances = new Dictionary<int, int>();
            portDistances.Add(Program.myPort, 0);
            GetRoutes();
        }

        public static void GetRoutes()
        {
            int distance = portDistances[Program.myPort];
            foreach (int buurPoort in Program.neighbours.Keys)
            {               
                if (!portDistances.ContainsKey(buurPoort))
                {
                    portDistances.Add(buurPoort, 1 + distance);
                    // Eigenlijk moet dit dus op de buurpoort uitgevoerd worden
                    GetRoutes(buurPoort);
                }
            }
        }

        public static void UpdateRoutes(int port)
        {

        }
    }*/

    class Server
    {
        public Server(int port)
        {
            // Luister op de opgegeven poort naar verbindingen
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();

            // Start een aparte thread op die verbindingen aanneemt
            new Thread(() => AcceptLoop(server)).Start();

            // Start een thread die de console input leest en afhandelt
            new Thread(() => ConsoleInputHandler()).Start();
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

        private void ConsoleInputHandler()
        {
            try
            {
                while (true)
                {
                    string input = Console.ReadLine();

                    // Gebruiker vraagt om routing table
                    if (input.StartsWith("R"))
                    {
                        // Print routing table

                        int distance = 0;
                        Console.WriteLine(Program.myPort + " " + distance + " local");
                        distance++;
                        foreach (KeyValuePair<int, Connection> kv in Program.neighbours)
                        {
                            Console.WriteLine(kv.Key + " " + distance + " " + kv.Key);
                        }
                    }
                    else if (input.StartsWith("B"))
                    {
                        string[] portAndMessage = input.Split(new char[] { ' ' }, 3);
                        int port = int.Parse(portAndMessage[1]);
                        string message = portAndMessage[2];

                        // Send Message to Port
                    }
                    else if (input.StartsWith("C"))
                    {
                        int port = int.Parse(input.Split()[1]);

                        // Connect to Port
                    }
                    else if (input.StartsWith("D"))
                    {
                        int port = int.Parse(input.Split()[1]);

                        // Disconnect from Port
                    }
                }
            }
            catch
            {
                // Verbinding is kennelijk verbroken
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
                    string input = Read.ReadLine();

                    if (input.StartsWith("RECOMPUTE"))
                    {
                        //Program.Recompute();
                        Console.WriteLine("RECOMPUTING");
                    }
                }
            }
            catch
            {
                // Verbinding is kennelijk verbroken
            }
        }
    }
}