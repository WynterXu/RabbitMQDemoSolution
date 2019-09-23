using System;
using System.Text;
using RabbitMQ.Client;

namespace Send01
{
    class Program
    {
        static void Main(string[] args)
        {
            RabbitMQUtility RabbitMQUtility = new RabbitMQUtility();

            string msg1 = "{\"RetCode\":1,\"ReturnMessage\":\"测试消息\"}";
            RabbitMQUtility.SendMessage(msg1, "t1", "g1", "u1");

            string msg2 = "{\"RetCode\":2,\"ReturnMessage\":\"测试消息\"}";
            RabbitMQUtility.SendMessage(msg2, "t1", "g2", "u2");

            string msg3 = "{\"RetCode\":3,\"ReturnMessage\":\"测试消息\"}";
            RabbitMQUtility.SendMessage(msg3, "t1", "g2", "u3");
        }

        void send(string[] args)
        {
            Console.WriteLine("Start Send");
            IConnectionFactory conFactory = new ConnectionFactory//创建连接工厂对象
            {
                HostName = "127.0.0.1",//IP地址
                Port = 5672,//端口号
                UserName = "guest",//用户账号
                Password = "guest"//用户密码
            };
            using (IConnection con = conFactory.CreateConnection())//创建连接对象
            {
                using (IModel channel = con.CreateModel())//创建连接会话对象,通信管道
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
                    while (true)
                    {
                        Console.WriteLine("Message content:");
                        String message = Console.ReadLine();
                        //消息内容
                        byte[] body = Encoding.UTF8.GetBytes(message);


                        IBasicProperties properties = channel.CreateBasicProperties();
                        properties.MessageId = Guid.NewGuid().ToString("N");
                        //发送消息
                        channel.BasicPublish(
                                               exchange: "",                //交换器名称
                                               routingKey: queueName,       //路由键
                                               basicProperties: properties,       //属性集合对象（14个成员，包含优先级、过期时间等等）
                                               body: body                   //消息内容
                                              );
                        Console.WriteLine("Success send:" + message);
                    }
                }
            }
        }
    }
    
}
