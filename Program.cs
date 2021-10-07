using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ_Test;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace RabbitMQ_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            new RPCServerRabbitMQ("localhost");
        }

        public class RPCServerRabbitMQ
        {
            public RPCServerRabbitMQ(string rabbithost)
            {
                var factory = new ConnectionFactory() { HostName = rabbithost };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "rpc_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
                    channel.BasicQos(0, 1, false);
                    var consumer = new EventingBasicConsumer(channel);
                    channel.BasicConsume(queue: "rpc_queue", autoAck: false, consumer: consumer);

                    consumer.Received += (model, ea) =>
                    {
                        object response = default;

                        var body = ea.Body.ToArray();
                        var props = ea.BasicProperties;
                        var replyProps = channel.CreateBasicProperties();
                        replyProps.CorrelationId = props.CorrelationId;

                        try
                        {
                            var message = FromByteArray<RPCRequest>(body);
                            var type = Type.GetType(message.Class);

                            //var Consf = type.GetConstructors().FirstOrDefault(r => r.GetParameters().Length > 0);

                            var Constructor = message.ParamsConstructor == null ? type.GetConstructor(Type.EmptyTypes) : type.GetConstructors().FirstOrDefault(r => r.GetParameters().Length > 0);
                            //            var ConstructorP=type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null,
                            //CallingConventions.HasThis, new Type[] { typeof(int) }, null);
                            var ClassObject = message.ParamsConstructor == null ? Constructor.Invoke(Array.Empty<object>()) : Constructor.Invoke(message.ParamsConstructor);

                            var info = type.GetMethod(message.Method);

                            response = message.ParamsMethod == null ? info.Invoke(ClassObject, Array.Empty<object>()) : info.Invoke(ClassObject, message.ParamsMethod);

                            Console.WriteLine("Invoked Class:{0} and Method:{1}", new object[] { message.Class, message.Method });
                        }
                        catch (Exception ex)
                        {
                            throw new Exception();
                        }
                        finally
                        {
                            var responseBytes = ToByteArray(response);
                            channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: responseBytes);
                            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                    };
                    Console.WriteLine("Press Enter");
                    Console.ReadLine();
                }
            }

            private byte[] ToByteArray<T>(T obj)
            {
                if (obj == null)
                    return null;
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                bf.Serialize(ms, obj);

                return ms.ToArray();
            }

            public T FromByteArray<T>(byte[] data)
            {
                if (data == null)
                    return default(T);
                BinaryFormatter bf = new BinaryFormatter();
                using (MemoryStream ms = new MemoryStream(data))
                {
                    object obj = bf.Deserialize(ms);
                    return (T)obj;
                }
            }
        }


    }
}
