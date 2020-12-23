using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SquidLogTransferDataServer.Objects;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Runtime.Serialization.Formatters.Binary;

namespace EventBusReceiveMessage
{
    class Program
    {
        static void Main(string[] args)
        {
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
            
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var packet = ByteArrayToObject(body);
                    Console.Write("Sendind e-mail... ");
                    SendEmail(packet);
                    Console.WriteLine("Completed");
                };
                channel.BasicConsume(queue: "message",
                                     autoAck: true,
                                     consumer: consumer);
                Console.ReadLine();
            }
        }

        private static EventBusMessage ByteArrayToObject(byte[] msg)
        {
            var binForm = new BinaryFormatter();
            MemoryStream memory = new MemoryStream();

            memory.Write(msg, 0, msg.Length);
            memory.Seek(0, SeekOrigin.Begin);

            var data = (EventBusMessage)binForm.Deserialize(memory);

            return data;
        }

        public static void SendEmail(EventBusMessage packet)
        {
            string to = "projectsquidaccesslog@gmail.com";
            string from = "projectsquidaccesslog@gmail.com";
            MailMessage message = new MailMessage(from, to);

            var totalTime = packet.TimesServer.ParseTimeClient +
                            packet.TimesServer.ReadTimeClient +
                            packet.TimesServer.TrasferTime +
                            packet.TimesServer.ParseTimeServer +
                            packet.TimesServer.ReadTimeServer +
                            packet.TimesServer.DbTime;

            message.Subject = packet.Message;
            message.Body = $"Tempo da conversão no client: {packet.TimesServer.ParseTimeClient} s\n" +
                           $"Tempo de leitura do arquivo no client: {packet.TimesServer.ReadTimeClient} s\n" +
                           $"Tempo de tranferência: {packet.TimesServer.TrasferTime} s\n" +
                           $"Tempo da conversão no server: {packet.TimesServer.ParseTimeServer} s\n" +
                           $"Tempo de leitura do arquivo no server: {packet.TimesServer.ReadTimeServer} s\n" +
                           $"Tempo de gravação no banco: {packet.TimesServer.DbTime} s\n" +
                           $"Tempo total: {totalTime} s\n";

            SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
            client.EnableSsl = true;
            NetworkCredential cred = new NetworkCredential("", "");
            client.Credentials = cred;

            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in CreateTestMessage2(): {0}",
                    ex.ToString());
            }
        }
    }
}
