using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace EtherChat_Server
{
    internal class Program
    {
        private static readonly ConcurrentDictionary<Socket, byte> clients = new ConcurrentDictionary<Socket, byte>();

        static void Main()
        {
            Socket socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            int port = 0;
            while (true)
            {
                Console.Write("请输入监听端口，留空以使用上次的端口：");
                string input = Console.ReadLine().Trim('\n');
                port = input.Equals("") ? Settings1.Default.PORT : int.Parse(input);
                if (port < 0 || port > 65535)
                {
                    Console.WriteLine("端口号无效，请重新输入。");
                    continue;
                }
                else
                {
                    try
                    {
                        socketWatch.Bind(new IPEndPoint(IPAddress.Any, port));
                        socketWatch.Listen(10);
                        if (Settings1.Default.PORT != port)
                        {
                            Settings1.Default.PORT = port;
                            Settings1.Default.Save();
                        }
                        break;
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("端口已被占用，请重新输入。");
                        continue;
                    }
                }
            }
            new Thread(Listen) { IsBackground = true }.Start(socketWatch);
            Console.WriteLine($"服务器已启动，监听端口：{port}");
            while (true) ;
        }

        static void Listen(object obj)
        {
            Socket socketWatch = (Socket)obj;
            while (true)
            {
                Socket socketSend = socketWatch.Accept();
                Console.WriteLine($"客户端连接成功：{socketSend.RemoteEndPoint}");
                new Thread(Receive) { IsBackground = true }.Start(socketSend);
            }
        }

        static void Receive(object obj)
        {
            Socket socketSend = (Socket)obj;
            try
            {
                clients.TryAdd(socketSend, 0);
                while (true)
                {
                    byte[] buffer = new byte[1024 * 1024 * 2];
                    int r = socketSend.Receive(buffer);
                    if (r == 0) break;
                    string str = Encoding.UTF8.GetString(buffer, 0, r);
                    if (str == "ServerHello")
                    {
                        socketSend.Send(Encoding.UTF8.GetBytes("ClientHello"));
                    }
                    else
                    {
                        Broadcast(str);
                        Console.WriteLine($"[转发消息] {DateTime.Now:HH:mm:ss} - {str}");
                    }
                }
            }
            catch
            {
                Console.WriteLine($"客户端异常断开：{socketSend.RemoteEndPoint}");
            }
            finally
            {
                clients.TryRemove(socketSend, out _);
                socketSend.Close();
            }
        }

        static void Broadcast(string msg)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            foreach (var client in clients.Keys.ToArray())
            {
                try
                {
                    if (client.Connected) client.Send(buffer);
                }
                catch
                {
                    clients.TryRemove(client, out _);
                    client.Close();
                }
            }
        }
    }
}