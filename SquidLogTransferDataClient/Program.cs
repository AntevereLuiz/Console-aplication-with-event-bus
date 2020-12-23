using SquidLogTransferDataClient.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace SquidLogTransferDataClient
{
    class Program
    {
        private const int port = 5050;
        private const string host = "127.0.0.1";

        static void Main(string[] args)
        {
            string file = @"C:\SquidLogTransferDataProject\SquidLogTransferData\SquidLogTransferDataClient\FileLog\access.log";

            if (File.Exists(file))
            {
                try
                {
                    String line;
                    var regex = new Regex(@"(.+?\s)(\S.*?\s)(.+?\s)(.+?\s)(.+?\s)(.+?\s)(.+?\s)(.+?\s)(.+?\s)(.+?$)");
                    var list = new List<AccessLogObject>();
                    int parseTime = 0;
                    int readingTime = 0;

                    Console.Write("Reading datas... ");

                    using (StreamReader sr = new StreamReader(file))
                    {
                        Console.WriteLine("Completed!");
                        Stopwatch watchTot = new Stopwatch();
                        Stopwatch watchParse = new Stopwatch();

                        Console.Write("Converting datas to byte array... ");
                        watchTot.Start();
                        while ((line = sr.ReadLine()) != null)
                        {
                            watchParse.Start();
                            AccessLogObject dataToAdd = ConvertToObject(line, regex);
                            list.Add(dataToAdd);
                            watchParse.Stop();
                        }
                        watchTot.Stop();

                        parseTime = watchParse.Elapsed.Seconds;
                        readingTime = watchTot.Elapsed.Seconds - parseTime;

                        Console.WriteLine("Completed!");
                        Console.Write("Send list to server... ");

                        var data = new Data();
                        data.Datas = new List<AccessLogObject>();
                        data.Datas.AddRange(list);
                        data.ReadTime = parseTime;
                        data.ParseTime = parseTime;
                        data.StartTrasferTime = DateTime.UtcNow;

                        SendListToServer(data);

                        Console.WriteLine("Completed!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("The file " + file + " was not found!");
            }
            Console.ReadKey();
        }

        private static AccessLogObject ConvertToObject(string line, Regex regex)
        {
            return new AccessLogObject()
            {
                Time = regex.Match(line).Groups[1].Value,
                Duration = regex.Match(line).Groups[2].Value,
                ClientAddress = regex.Match(line).Groups[3].Value,
                ResultCode = regex.Match(line).Groups[4].Value,
                Bytes = regex.Match(line).Groups[5].Value,
                RequestMethod = regex.Match(line).Groups[6].Value,
                Url = regex.Match(line).Groups[7].Value,
                User = regex.Match(line).Groups[8].Value,
                HierarchyCode = regex.Match(line).Groups[9].Value,
                Type = regex.Match(line).Groups[10].Value
            };
        }

        private static void SendListToServer(Data data)
        {
            var client = new TcpClient(host, port);
            var ns = client.GetStream();

            var byteArray = ConvertToByteArray(data);

            ns.Write(byteArray, 0, byteArray.Length);

            ns.Close();
            client.Close();
        }

        public static byte[] ConvertToByteArray(Data data)
        {
            BinaryFormatter binFormat = new BinaryFormatter();
            MemoryStream mStream = new MemoryStream();
            binFormat.Serialize(mStream, data);
            byte[] byteArray = mStream.ToArray();

            return byteArray;
        }
    }
}