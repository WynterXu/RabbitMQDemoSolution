using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace Receive1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Receive Ready");
            IConnectionFactory connFactory = new ConnectionFactory//创建连接工厂对象
            {
                HostName = "127.0.0.1",//IP地址
                Port = 5672,//端口号
                UserName = "guest",//用户账号
                Password = "guest"//用户密码
            };
            using (IConnection conn = connFactory.CreateConnection())
            {
                using (IModel channel = conn.CreateModel())
                {
                    String queueName = String.Empty;
                    if (args.Length > 0)
                        queueName = args[0];
                    else
                        queueName = "queue1";
                    //声明一个队列
                    channel.QueueDeclare(
                      queue: queueName,//消息队列名称
                      durable: false,//是否缓存
                      exclusive: false,
                      autoDelete: false,
                      arguments: null
                       );
                    //创建消费者对象
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) => //绑定监听事件
                    {
                        byte[] message = ea.Body;//接收到的消息
                        Console.WriteLine(@"Received Message:");
                        Console.WriteLine(Encoding.UTF8.GetString(message));
                    };
                    //消费者开启监听
                    channel.BasicConsume(
                        queue: queueName,   //队列名
                        autoAck: true,      //是否自动确认消息,true自动确认,false 不自动要手动调用,建议设置为false
                        consumer: consumer  //消费者对象
                        );
                    Console.ReadKey();
                }
            }
        }
    }
}