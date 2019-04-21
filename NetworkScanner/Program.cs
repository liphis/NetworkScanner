using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace NetworkScanner
{
    internal class Program
    {
        // settings
        private static readonly string IP_BASE = "192.168.0.{0}";
        private static readonly bool RESOLVE_NAMES = true;
        private static readonly bool SHOW_DOWN = false;
        private static readonly bool SHOW_NULL = true;
        private static readonly bool SHOW_UP = true;
        private static readonly int TIMEOUT = 100;

        private static int up_count = 0;
        private static readonly string TITLE = Console.Title;
        private static readonly CountdownEvent COUNTDOWN = new CountdownEvent(1);
        private static readonly object LOCK = new object();
        private static readonly Stopwatch STOPWATCH = new Stopwatch();

        private static void Main(string[] args)
        {
            STOPWATCH.Start();

            Console.Title = "Network Scanner v1";

            Console.WriteLine();
            Console.WriteLine(" ------------------------------------------------------------------------------------------------------------");
            Console.WriteLine(" | {0,-15} | {1,-60} | {2,-10} | {3,-10} |", "HOST IPv4", "HOST NAME", "RESPONSE", "RT TIME");
            Console.WriteLine(" |-----------------|--------------------------------------------------------------|------------|------------|");

            if (args.Length != 0)
            {
                foreach (string ip in args)
                {
                    COUNTDOWN.AddCount();

                    Ping p = new Ping();
                    p.PingCompleted += new PingCompletedEventHandler(P_PingCompleted);
                    p.SendAsync(ip, TIMEOUT, ip);
                }
            }
            else
            {
                for (int i = 0; i <= 255; i++)
                {
                    COUNTDOWN.AddCount();

                    string ip = String.Format(IP_BASE, i);

                    Ping p = new Ping();
                    p.PingCompleted += new PingCompletedEventHandler(P_PingCompleted);
                    p.SendAsync(ip, TIMEOUT, ip);
                }
            }
            COUNTDOWN.Signal();
            COUNTDOWN.Wait();
            STOPWATCH.Stop();

            Console.WriteLine(" ------------------------------------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(" took {0} milliseconds. {1} host(s) up.", STOPWATCH.ElapsedMilliseconds, up_count);
            Console.ReadLine();

            Console.Title = TITLE;
        }

        private static void P_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            string ip;
            string name;

            if (IPAddress.TryParse((string)e.UserState, out _))
            {
                ip = (string)e.UserState;

                try
                {
                    if ((RESOLVE_NAMES) && ((e.Reply != null) && (e.Reply.Status == IPStatus.Success)))
                    {
                        IPHostEntry hostEntry = Dns.GetHostEntry(ip);
                        name = hostEntry.HostName;
                    }
                    else
                    {
                        name = "";
                    }
                }
                catch (SocketException)
                {
                    name = "";
                }
            }
            else
            {
                name = (string)e.UserState;

                try
                {
                    IPHostEntry hostEntry = Dns.GetHostEntry(name);
                    ip = hostEntry.AddressList[hostEntry.AddressList.Length - 1].ToString();
                }
                catch (SocketException)
                {
                    ip = "";
                }
                catch (IndexOutOfRangeException)
                {
                    ip = "";
                }
            }

            if ((e.Reply != null) && (e.Reply.Status == IPStatus.Success))
            {
                if (SHOW_UP)
                {
                    Console.WriteLine(" | {0,-15} | {1,-60} | {2,-10} | {3,7} ms |", ip, name, e.Reply.Status, e.Reply.RoundtripTime);
                }

                lock (LOCK)
                {
                    up_count++;
                }
            }
            else if (e.Reply == null)
            {
                if (SHOW_NULL)
                {
                    Console.WriteLine(" | {0,-15} | {1,-60} | {2,-10} | {3,10} |", ip, name, "", "");
                }
            }
            else
            {
                if (SHOW_DOWN)
                {
                    Console.WriteLine(" | {0,-15} | {1,-60} | {2,-10} | {3,7} ms |", ip, name, e.Reply.Status, TIMEOUT);
                }
            }

            COUNTDOWN.Signal();
        }
    }
}
