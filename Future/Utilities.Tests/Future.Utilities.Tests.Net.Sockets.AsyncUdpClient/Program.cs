using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Future.Utilities.Net.Sockets;

namespace Future.Utilities.Tests.Net.Sockets.AsyncUdpClient
{
    class Program
    {
        static Utilities.Net.Sockets.AsyncUdpClient _client;

        static void Main(string[] args)
        {
            try
            {
                var config = new AsyncUdpClientConfiguration();
                //config.UseSsl = true;
                //config.SslTargetHost = "Cowboy";
                //config.SslClientCertificates.Add(new System.Security.Cryptography.X509Certificates.X509Certificate2(@"D:\\Cowboy.cer"));
                //config.SslPolicyErrorsBypassed = false;

                //config.FrameBuilder = new FixedLengthFrameBuilder(20000);
                config.FrameBuilder = new RawBufferFrameBuilder();
                //config.FrameBuilder = new LineBasedFrameBuilder();
                //config.FrameBuilder = new LengthPrefixedFrameBuilder();
                //config.FrameBuilder = new LengthFieldBasedFrameBuilder();

                IPEndPoint local = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 22222);
                IPEndPoint remote = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 33333);
                _client = new Utilities.Net.Sockets.AsyncUdpClient(local, new UdpClientEventDispatcher(), config);
                _client.Start().Wait();

                Console.WriteLine("Udp client has running to local [{0}].", local);
                Console.WriteLine("Type something to send to remote [{0}]...", remote);
                while (true)
                {
                    try
                    {
                        string text = Console.ReadLine();
                        if (text == "quit")
                            break;
                        Task.Run(async () =>
                        {
                            if (text == "many")
                            {
                                text = new string('x', 8192);
                                for (int i = 0; i < 1000000; i++)
                                {
                                    await _client.SendAsync(Encoding.UTF8.GetBytes(text), remote);
                                    Console.WriteLine("Client [{0}] send text -> [{1}].", _client.LocalEndPoint, text);
                                }
                            }
                            else if (text == "big1k")
                            {
                                text = new string('x', 1024 * 1);
                                await _client.SendAsync(Encoding.UTF8.GetBytes(text), remote);
                                Console.WriteLine("Client [{0}] send text -> [{1} Bytes].", _client.LocalEndPoint, text.Length);
                            }
                            else if (text == "big10k")
                            {
                                text = new string('x', 1024 * 10);
                                await _client.SendAsync(Encoding.UTF8.GetBytes(text), remote);
                                Console.WriteLine("Client [{0}] send text -> [{1} Bytes].", _client.LocalEndPoint, text.Length);
                            }
                            else if (text == "big100k")
                            {
                                text = new string('x', 1024 * 100);
                                await _client.SendAsync(Encoding.UTF8.GetBytes(text), remote);
                                Console.WriteLine("Client [{0}] send text -> [{1} Bytes].", _client.LocalEndPoint, text.Length);
                            }
                            else if (text == "big1m")
                            {
                                text = new string('x', 1024 * 1024 * 1);
                                await _client.SendAsync(Encoding.UTF8.GetBytes(text), remote);
                                Console.WriteLine("Client [{0}] send text -> [{1} Bytes].", _client.LocalEndPoint, text.Length);
                            }
                            else if (text == "big10m")
                            {
                                text = new string('x', 1024 * 1024 * 10);
                                await _client.SendAsync(Encoding.UTF8.GetBytes(text), remote);
                                Console.WriteLine("Client [{0}] send text -> [{1} Bytes].", _client.LocalEndPoint, text.Length);
                            }
                            else if (text == "big100m")
                            {
                                text = new string('x', 1024 * 1024 * 100);
                                await _client.SendAsync(Encoding.UTF8.GetBytes(text), remote);
                                Console.WriteLine("Client [{0}] send text -> [{1} Bytes].", _client.LocalEndPoint, text.Length);
                            }
                            else if (text == "big1g")
                            {
                                text = new string('x', 1024 * 1024 * 1024);
                                await _client.SendAsync(Encoding.UTF8.GetBytes(text), remote);
                                Console.WriteLine("Client [{0}] send text -> [{1} Bytes].", _client.LocalEndPoint, text.Length);
                            }
                            else
                            {
                                await _client.SendAsync(Encoding.UTF8.GetBytes(text), remote);
                                Console.WriteLine("Client [{0}] send text -> [{1} Bytes].", _client.LocalEndPoint, text.Length);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message, ex);
                    }
                }

                _client.Shutdown();
                Console.WriteLine("Udp client has disconnected from server [{0}].", local);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, ex);
            }

            Console.ReadKey();
        }
    }
}
