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
        public static int myPort;                                                                               // port number
        public static Dictionary<int, Connection> neighbours = new Dictionary<int, Connection>();               // connecties met buren 
        public static Dictionary<int, int> preferredNeighbours = new Dictionary<int, int>();                    // key = eindbestemming en value = via welke buur het snelste is
        public static Dictionary<int, int> distanceToPort = new Dictionary<int, int>();                         // key = eindbestemming en value = afstand van huidige tot eindbestemming
        public static Dictionary<Tuple<int, int>, int> ndis = new Dictionary<Tuple<int, int>, int>();           // key = tuple van ports waartussen we de afstand willen weten, minstens een van deze poorts is een neighbour, value = afstand tussen deze twee ports
        public static HashSet<int> allNodes = new HashSet<int>();                                               // Alle bekende nodes in het netwerk

        static void Main(string[] args)
        {
            // haalt het poortnummer op van de huidige port
            myPort = int.Parse(args[0]);
            // Geef console het poortnummer als titel
            Console.Title = "NetChange " + myPort;
            // maak een nieuwe server aan van de huidige port
            new Server(myPort);
            // zet de afstand naar de huidige poort op 0
            distanceToPort[myPort] = 0;
            // zet de huidige port als beste buur als de eindbestemming de huidige poort is
            preferredNeighbours[myPort] = myPort;
            // Voeg de huidige port toe aan de lijst van nodes
            allNodes.Add(myPort);
            // maak connecties aan met directe buren
            for (int i = 1; i < args.Length; i++)
            {
                // haal portnummer op
                int port = int.Parse(args[i]);
                // voeg hem toe aan de dictionary van de buren
                AddConnectionToDictionary(port, new Connection(port));
                // voeg de port toe aan de lijst van nodes die bekend zijn
                //allNodes.Add(port);
                // zet de afstand op 1 omdat hij een directe buur is
                //distanceToPort[port] = 1;
                // zet de preferred neighbour op zichzelf
                //preferredNeighbours[port] = port;
            }
            Init();
        }

        // voeg een port toe aan de burendictionary als deze nog niet bestaat
        public static void AddConnectionToDictionary(int port, Connection connection)
        {
            if (!neighbours.ContainsKey(port)) // TODO : let op duplicate code
            {
                neighbours.Add(port, connection);
                //allNodes.Add(port);
                //distanceToPort[port] = 1;
                //preferredNeighbours[port] = port;
            }
        }

        // methode voor het verkrijgen van de beste buur bij een gegeven eindbestemming
        private static int GetClosestNeighbour(int port)
        {
            Console.WriteLine("GETCLOSESTNEIGHBOUR");
            // initialize the closest distance on the size of the network and the best neighbour on -1 (undefined)
            int closest = MaxNetworkSize();
            int bestNeighbour = -1;
            // loop through the neighbours and get the distances from this neighbour to the port from the ndis
            foreach (KeyValuePair<int,Connection> kv in neighbours)
            {
                int distance = ndis[Tuple.Create(kv.Key, port)];
                Console.WriteLine("Getting distance from tuple: (" + kv.Key + ", " + port + "), result: " + distance);
                // if this distance is the smallest, update the closest and bestneighbur values
                if (distance < closest)
                {
                    bestNeighbour = kv.Key;
                    closest = distance;
                }
            }
            // return the found best neighbour
            return bestNeighbour;
        }

        public static void Init()
        {
            // loop through all the known nodes and create all combinations in the ndis dictionary with the current maxnetworksize
            for (int i = 0; i < allNodes.Count; i++)
            {
                for (int j = 1; j < allNodes.Count; j++)
                {
                    if (j != i)
                    {
                        ndis[Tuple.Create(i, j)] = MaxNetworkSize();
                        ndis[Tuple.Create(j, i)] = MaxNetworkSize();
                    }
                }
            }

            // create a mydist message from this port to itself with distance 0 and send it to all the neighbours
            string message = "MyDist " + myPort + " 0 " + myPort;

            foreach (KeyValuePair<int,Connection> neighbour in neighbours)
            {
                neighbour.Value.Write.WriteLine(message);
            }
        }

        // herberekent afstanden van het netwerk
        public static void Recompute(int port)
        {
            Console.WriteLine("RECOMPUTE YAAAAY");

            int oldDistance = distanceToPort[port];
            
            // checkt of de meegegeven port de huidige port is en zet daarvan de afstand op 0
            if (port == myPort)
            {
                distanceToPort[port] = 0;
                preferredNeighbours[port] = port;
            }
            // bereken de afstand en tel daar 1 bij op
            else
            {
                int bestNeighbour = GetClosestNeighbour(port);
                Console.WriteLine("BestN: " + bestNeighbour);
                preferredNeighbours[port] = bestNeighbour;
                distanceToPort[port] = ndis[Tuple.Create(bestNeighbour, port)] + 1;
            }

            // Als de afstand veranderd is, stuur dan een bericht naar ale buren
            if (distanceToPort[port] != oldDistance)
            {
                string message = "MyDist " + port + " " + distanceToPort[port] + " " + myPort;

                foreach (KeyValuePair<int,Connection> neighbour in neighbours)
                {
                    neighbour.Value.Write.WriteLine(message);
                }
            }
        }

        // returns the size of the network, the max distance a node can be from another one
        public static int MaxNetworkSize()
        {
            return allNodes.Count + 1;
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
                        Console.WriteLine(Program.myPort + " 0 local");
                        foreach (KeyValuePair<int, int> kv in Program.distanceToPort)
                        {
                            if (kv.Key != Program.myPort)
                            {
                                Console.WriteLine(kv.Key + " " + kv.Value + " " + Program.preferredNeighbours[kv.Key]);
                            }
                        }
                        string output = "Known nodes:";
                        foreach(int i in Program.allNodes)
                        {
                            output += " " + i;
                        }
                        Console.WriteLine(output);
                    }
                    else if (input.StartsWith("B"))
                    {
                        /*
                        string[] portAndMessage = input.Split(new char[] { ' ' }, 3);
                        int port = int.Parse(portAndMessage[1]);
                        string message = portAndMessage[2];

                        // Send Message to Port
                        int prefN = Program.preferredNeighbours[port];

                        Console.WriteLine("Bericht voor " + port + " doorgestuurd naar " + prefN);

                        Program.neighbours[prefN].Write.WriteLine("B " + port + " " + message);
                        */
                        // TODO : error afvangen als hij poort niet kent
                    }
                    else if (input.StartsWith("C"))
                    {
                        /*
                        int port = int.Parse(input.Split()[1]);

                        Program.neighbours.Add(port, new Connection(port));
                        Program.neighbours[port].Write.WriteLine("Connect " + Program.myPort);
                        Program.Recompute();
                        */
                        // Connect to Port
                    }
                    else if (input.StartsWith("D"))
                    {
                        /*
                        int port = int.Parse(input.Split()[1]);

                        Program.neighbours.Remove(port);
                        // Verwijder in de preferredNeighbours dictionary ieder element met als value de port die verwijderd moet worden
                        foreach (KeyValuePair<int,int> kv in Program.preferredNeighbours.Where(kvp => kvp.Value == port).ToList())
                        {
                            if (kv.Value == port)
                            {
                                Program.preferredNeighbours.Remove(kv.Key);
                            }
                        }
                        Program.Recompute();
                        // Disconnect from Port
                        */
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
                    string[] splittedInput = input.Split();
                    // Als A verbindt met B, voegt B ook nog A toe aan zijn dictionaries
                    if (input.StartsWith("Poort"))
                    {
                        Program.AddConnectionToDictionary(int.Parse(splittedInput[1]), this);
                    }
                    if (input.StartsWith("MyDist"))
                    {
                        // get the toPort, distance and fromPort values from the message
                        int toPort = int.Parse(splittedInput[1]);
                        int distance = int.Parse(splittedInput[2]);
                        int fromPort = int.Parse(splittedInput[3]);
                        Console.WriteLine("MyDist ontvangen van " + fromPort + " tot " + toPort + " met afstand " + distance);

                        // if the message is about a node we do not yet know, add it to the disctionaries
                        if (!Program.allNodes.Contains(toPort))
                        {
                            Console.WriteLine("Poort: " + toPort + " ken ik niet!");
                            Program.allNodes.Add(toPort);
                            Program.distanceToPort[toPort] = Program.MaxNetworkSize();
                            Program.preferredNeighbours[toPort] = -1;
                            foreach (KeyValuePair<int, Connection> n in Program.neighbours)
                            {
                                Program.ndis[Tuple.Create(toPort, n.Key)] = Program.MaxNetworkSize();
                                Program.ndis[Tuple.Create(n.Key, toPort)] = Program.MaxNetworkSize();
                            }
                        }

                        // set the distance to the ndis from the toport to the fromport and the other way around
                        Program.ndis[Tuple.Create(fromPort, toPort)] = distance;
                        Program.ndis[Tuple.Create(toPort, fromPort)] = distance;
                        Console.WriteLine("Setting distance " + distance + " to tuple (" + fromPort + ", " + toPort + ")");
                        Console.WriteLine("Setting distance " + distance + " to tuple (" + toPort + ", " + fromPort + ")");
                        // recompute
                        Program.Recompute(toPort);
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