using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;

namespace Receive1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start");
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
                      queue: queueName,     //消息队列名称
                      durable: false,       //是否持久化,是否缓存
                      exclusive: false,     //是否排外
                      autoDelete: false,    //是否自动删除
                      arguments: null       //队列中的消息什么时候会自动被删除
                       );
                    //Qos，服务质量保证：告诉Rabbit每次只能向消费者发送一条信息,再消费者未确认之前,不再向他发送信息
                    channel.BasicQos(
                        prefetchSize: 0,
                        prefetchCount: 1,   //不要同时给一个消费者推送多于N个消息，即一旦有N个消息还没有ack，则该consumer将block掉，直到有消息ack
                        global: false);     //是否将上面设置应用于channel，简单点说，就是上面限制是channel级别的还是consumer级别

                    //创建消费者对象
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        Thread.Sleep((new Random().Next(1, 6)) * 1000);//随机等待,实现能者多劳,
                        byte[] message = ea.Body;//接收到的消息
                        Console.WriteLine("接收到信息为:" + Encoding.UTF8.GetString(message));

                        //返回消息确认
                        channel.BasicAck(
                                        ea.DeliveryTag, //该消息的index
                                        true            //是否批量.true:将一次性ack所有小于deliveryTag的消息。
                                        );
                    };
                    //消费者开启监听
                    //将autoAck设置false 关闭自动确认
                    channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
                    Console.ReadKey();
                }
            }
        }
    }
}