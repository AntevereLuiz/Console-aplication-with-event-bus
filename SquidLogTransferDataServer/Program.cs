using RabbitMQ.Client;
using SquidLogTransferDataClient.Objects;
using SquidLogTransferDataServer.Objects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

public class Program
{
    public static void Main(String[] args)
    {
        Int32 port = 5050;
        TcpListener server = null;
        var localAddr = IPAddress.Parse("127.0.0.1");
        Byte[] byteArray = new Byte[10000000];
        var watchRead = new Stopwatch();
        var watchParse = new Stopwatch();
        var watchDb = new Stopwatch();

        try
        {
            while (true)
            {
                Console.Write("Waiting for a connection... ");
                server = new TcpListener(localAddr, port);
                server.Start();
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connected!");

                Console.Write("Receiving datas... ");
                NetworkStream stream = client.GetStream();
                var finishTrasferTime = DateTime.UtcNow;
                Console.WriteLine("Completed!");

                Console.Write("Reading datas... ");
                watchRead.Start();
                stream.Read(byteArray, 0, byteArray.Length);
                watchRead.Stop();
                Console.WriteLine("Completed!");

                watchParse.Start();
                Console.Write("Converting to object... ");
                var list = ByteArrayToObject(byteArray);
                Console.WriteLine("Completed!");
                watchParse.Stop();

                watchDb.Start();
                Console.Write("Saiving in DB... ");
                SaveInDb(list.Datas);
                Console.WriteLine("Completed!");
                watchDb.Stop();

                Console.Write("Closing Connection... ");
                client.Close();
                stream.Close();
                Console.WriteLine("Completed!");

                var times = new Times() {
                    ReadTimeServer = watchRead.Elapsed.Seconds,
                    ParseTimeServer = watchParse.Elapsed.Seconds,
                    DbTime = watchDb.Elapsed.Seconds,
                    TrasferTime = finishTrasferTime.TimeOfDay.Seconds - list.StartTrasferTime.TimeOfDay.Seconds,
                    ParseTimeClient = list.ParseTime,
                    ReadTimeClient = list.ReadTime
                };

                SendMessageEventBus("Processo finalizado com sucesso!", times);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            server.Stop();
            Console.ReadLine();
        }
 
    }

    private static Data ByteArrayToObject(byte[] byteArray)
    {
        var binForm = new BinaryFormatter();
        MemoryStream memory = new MemoryStream();
        
        memory.Write(byteArray, 0, byteArray.Length);
        memory.Seek(0, SeekOrigin.Begin);
        
        var data = (Data)binForm.Deserialize(memory);

        return data;
    }

    public static byte[] ConvertToByteArray(EventBusMessage times)
    {
        BinaryFormatter binFormat = new BinaryFormatter();
        MemoryStream mStream = new MemoryStream();
        binFormat.Serialize(mStream, times);
        byte[] bytes = mStream.ToArray();

        return bytes;
    }

    public static void SaveInDb(List<AccessLogObject> list)
    {
        string connectionString = @"Data Source=.,7070;Database=AccessLog;User Id=sa;Password=sqlFATEC()_";

        try { 
            SqlConnection connection = new SqlConnection(connectionString);
            SqlBulkCopy objbulk = new SqlBulkCopy(connection);

            DataTable tbl = new DataTable();
            tbl.Columns.Add(new DataColumn("Time", typeof(string)));
            tbl.Columns.Add(new DataColumn("Duration", typeof(string)));
            tbl.Columns.Add(new DataColumn("ClientAddress", typeof(string)));
            tbl.Columns.Add(new DataColumn("ResultCode", typeof(string)));
            tbl.Columns.Add(new DataColumn("Bytes", typeof(string)));
            tbl.Columns.Add(new DataColumn("RequestMethod", typeof(string)));
            tbl.Columns.Add(new DataColumn("Url", typeof(string)));
            tbl.Columns.Add(new DataColumn("Users", typeof(string)));
            tbl.Columns.Add(new DataColumn("HierarchyCode", typeof(string)));
            tbl.Columns.Add(new DataColumn("Type", typeof(string)));

            foreach(var line in list)
            {
                DataRow dr = tbl.NewRow();
                dr["Time"] = line.Time;
                dr["Duration"] = line.Duration;
                dr["ClientAddress"] = line.ClientAddress;
                dr["ResultCode"] = line.ResultCode;
                dr["Bytes"] = line.Bytes;
                dr["RequestMethod"] = line.RequestMethod;
                dr["Url"] = line.Url;
                dr["Users"] = line.User;
                dr["HierarchyCode"] = line.HierarchyCode;
                dr["Type"] = line.Type;

                tbl.Rows.Add(dr);
            }

            objbulk.DestinationTableName = "Log";

            objbulk.ColumnMappings.Add("Time", "Time");
            objbulk.ColumnMappings.Add("Duration", "Duration");
            objbulk.ColumnMappings.Add("ClientAddress", "ClientAddress");
            objbulk.ColumnMappings.Add("ResultCode", "ResultCode");
            objbulk.ColumnMappings.Add("Bytes", "Bytes");
            objbulk.ColumnMappings.Add("RequestMethod", "RequestMethod");
            objbulk.ColumnMappings.Add("Url", "Url");
            objbulk.ColumnMappings.Add("Users", "Users");
            objbulk.ColumnMappings.Add("HierarchyCode", "HierarchyCode");
            objbulk.ColumnMappings.Add("Type", "Type");

            connection.Open();
            objbulk.WriteToServer(tbl);
            connection.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public static void SendMessageEventBus(string message, Times times)
    {
        Console.Write("Sending message... ");
        var packet = new EventBusMessage()
        {
            Message = message,
            TimesServer = times
        };

        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "message",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var body = ConvertToByteArray(packet);

            channel.BasicPublish(exchange: "",
                                 routingKey: "message",
                                 basicProperties: null,
                                 body: body);
        }

        Console.WriteLine("Completed!");
        Console.ReadLine();
    }
}