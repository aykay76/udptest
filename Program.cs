using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace udptest
{
    class Program
    {
        static ManualResetEvent quitEvent = new ManualResetEvent(false);

        public static int Main(string[] args)
        {
            bool multicast = false;
            bool broadcast = false;
            bool send = false;
            bool receive = false;
            string address = string.Empty;
            int delay = 1000;

            foreach (string arg in args)
            {
                if (arg == "/multicast" || arg == "-multicast")
                {
                    Console.WriteLine("I will multicast");
                    multicast = true;
                }
                if (arg == "/broadcast" || arg == "-broadcast")
                {
                    Console.WriteLine("I will broadcast");
                    broadcast = true;
                }
                if (arg == "/send" || arg == "-send")
                {
                    Console.WriteLine("I will send");
                    send = true;
                }
                if (arg == "/receive" || arg == "-receive")
                {
                    Console.WriteLine("I will receive");
                    receive = true;
                }
                if (arg.StartsWith("/address") || arg.StartsWith("-address"))
                {
                    address = arg.Substring(9);
                }
                if (arg.StartsWith("/delay") || arg.StartsWith("-delay"))
                {
                    delay = int.Parse(arg.Substring(7));
                }
            }

            if (!send && !receive)
            {
                Console.WriteLine("You must use /send or /receive to take one role");
                return 1223;
            }

            Console.WriteLine("Hello World!");

            if (receive)
            {
                Console.WriteLine("Waiting... run again with /send and/or /multicast or /broadcast to initiate a sender");
                UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 9876));

                if (multicast)
                {
                    client.JoinMulticastGroup(IPAddress.Parse("239.128.128.128"));
                }

                Task t = Task.Run(async () =>
                {
                    while (true)
                    {
                        UdpReceiveResult result = await client.ReceiveAsync();
                        byte[] buffer = result.Buffer;
                        IPEndPoint remoteEndpoint = result.RemoteEndPoint;
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer));
                        System.Threading.Thread.Sleep(delay);
                    }
                });

                Console.WriteLine("Press any key to quit it");
                Console.ReadKey();
            }

            if (send)
            {
                int packet = 1;
                UdpClient sender = new UdpClient(9877);

                while (true)
                {
                    if (multicast)
                    {
                        byte[] datagram = System.Text.Encoding.UTF8.GetBytes($"Multicast {packet}");
                        Task<int> t = sender.SendAsync(datagram, datagram.Length, new IPEndPoint(IPAddress.Parse("239.128.128.128"), 9876));
                        t.GetAwaiter().GetResult();
                    }
                    if (broadcast)
                    {
                        Console.WriteLine($"Broadcast {packet}");
                        byte[] datagram = System.Text.Encoding.UTF8.GetBytes($"Broadcast {packet}");
                        sender.Send(datagram, datagram.Length, new IPEndPoint(IPAddress.Parse("255.255.255.255"), 9876));
                    }
                    if (!multicast && !broadcast)
                    {
                        byte[] datagram = System.Text.Encoding.UTF8.GetBytes($"Specific {packet}");
                        Task<int> t = sender.SendAsync(datagram, datagram.Length, new IPEndPoint(IPAddress.Parse(address), 9876));
                        t.GetAwaiter().GetResult();
                    }
                    System.Threading.Thread.Sleep(delay);
                    packet++;
                }
            }

            return 0;
        }
    }
}
