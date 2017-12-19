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

        public static List<Tuple<int, int>> ALLENODES = new List<Tuple<int, int>>();


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
            ALLENODES = RecomputeDijkstra(myPort);
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
            //Console.WriteLine("GETCLOSESTNEIGHBOUR");
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

        // Onze eigen recompute via het dijkstra algoritme
        public static List<Tuple<int, int>> RecomputeDijkstra(int port)
        {
            // cost-so-far = 0, huidige (begin)poort
            int distance = 0;
            Tuple<int, int> startNode = Tuple.Create(0, port);
            List<Tuple<int, int>> beenhere = new List<Tuple<int, int>>();
            List<Tuple<int, int>> allNodes = new List<Tuple<int, int>>();
            beenhere.Add(startNode);

            while (beenhere.Count != 0)
            {               
                // pop 
                Tuple<int, int> firstElem = beenhere[0];
                beenhere.RemoveAt(0);
                //Console.WriteLine("First Element: "+firstElem);
                // Dijkstra heeft deze node al eerder gezien
                if (beenhere.Contains(firstElem))
                {
                    continue;
                }

                // voeg node toe aan closed lijst
                beenhere.Add(firstElem);

                // Verhoog distance met 1
                distance++;

                // Verkrijg buren van huidige node en voeg ze toe aan de beenhere lijst 
                foreach (KeyValuePair<int, Connection> neighbour in neighbours)
                {
                    //Console.WriteLine("Neighbour: " + neighbour);
                    if (!beenhere.Contains(Tuple.Create(distance, neighbour.Key)))
                    {
                        allNodes.Add(Tuple.Create(distance, neighbour.Key));
                    }
                }
            }
            Console.WriteLine("DIJKSTRA COMPLETED");
            Console.WriteLine(allNodes);
            return allNodes;
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

                        // TODO : delete the known nodes message
                        string output = "Known nodes:";
                        foreach(int i in Program.allNodes)
                        {
                            output += " " + i;
                        }
                        Console.WriteLine(output);
                    }
                    else if (input.StartsWith("B"))
                    {
                        // split the input in three parts, the B, the port and the message
                        string[] portAndMessage = input.Split(new char[] { ' ' }, 3);
                        int port = int.Parse(portAndMessage[1]);
                        string message = portAndMessage[2];

                        // if we don't know the destination node, write this to the console
                        if (!Program.allNodes.Contains(port))
                        {
                            Console.WriteLine("Poort " + port + " is niet bekend");
                        }
                        else
                        {
                            // get the preferred neighbour for the destination port
                            int prefN = Program.preferredNeighbours[port];
                            // write to the console that we sent the message to the preferred neighbour
                            Console.WriteLine("Bericht voor " + port + " doorgestuurd naar " + prefN);
                            // send the message to the preferred neighbour
                            Program.neighbours[prefN].Write.WriteLine("B " + port + " " + message);
                        }
                    }
                    else if (input.StartsWith("C"))
                    {
                        // get the port we want to connect to
                        int port = int.Parse(input.Split()[1]);
                        // add this port to the neighbour dictionary
                        Program.AddConnectionToDictionary(port, new Connection(port));
                        // add it to allnodes
                        Program.allNodes.Add(port);
                        // set the distance to this port to 1
                        Program.distanceToPort[port] = 1;
                        // and the preferred neighbour to itself
                        Program.preferredNeighbours[port] = port;
                        // write a c myport message to the port we want to connect to
                        Program.neighbours[port].Write.WriteLine("C " + Program.myPort);
                        // create a mydist message to this new neighbour
                        string message = "MyDist " + port + " " + Program.distanceToPort[port] + " " + Program.myPort;
                        // write the mydist to all the neighbours
                        foreach (KeyValuePair<int, Connection> n in Program.neighbours)
                        {
                            n.Value.Write.WriteLine(message);
                        }
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
                        int port = int.Parse(splittedInput[1]);
                        Program.AddConnectionToDictionary(port, this);
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
                        //Program.Recompute(toPort);
                        Program.ALLENODES = Program.RecomputeDijkstra(toPort);

                        
                    }
                    if (input.StartsWith("B"))
                    {
                        // split the input in three parts, the B, the port and the message
                        string[] portAndMessage = input.Split(new char[] { ' ' }, 3);
                        int port = int.Parse(portAndMessage[1]);
                        string message = portAndMessage[2];

                        if (port == Program.myPort)
                        {
                            Console.WriteLine(message);
                        }
                        else
                        {
                            // get the preferred neighbour for the destination port
                            int prefN = Program.preferredNeighbours[port];
                            // write to the console that we sent the message to the preferred neighbour
                            Console.WriteLine("Bericht voor " + port + " doorgestuurd naar " + prefN);
                            // send the message to the preferred neighbour
                            Program.neighbours[prefN].Write.WriteLine("B " + port + " " + message);
                        }
                    }
                    if (input.StartsWith("C"))
                    {
                        // get the port we want to connect to
                        int port = int.Parse(input.Split()[1]);
                        // add this port to the neighbour dictionary
                        Program.AddConnectionToDictionary(port, new Connection(port));
                        // add it to allnodes
                        Program.allNodes.Add(port);
                        // set the distance to this port to 1
                        Program.distanceToPort[port] = 1;
                        // and the preferred neighbour to itself
                        Program.preferredNeighbours[port] = port;
                        // create a mydist message to this new neighbour
                        string message = "MyDist " + port + " " + Program.distanceToPort[port] + " " + Program.myPort;
                        // write the mydist to all the neighbours
                        foreach (KeyValuePair<int, Connection> n in Program.neighbours)
                        {
                            n.Value.Write.WriteLine(message);
                        }
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