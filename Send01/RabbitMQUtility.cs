using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Send01
{
    public class RabbitMQUtility
    {
        private static IConnectionFactory ConnectionFactory { get; set; }

        /// <summary>
        /// 初始化连接工厂对象
        /// </summary>
        /// <param name="_connectionFactory"></param>
        public RabbitMQUtility()
        {
            ConnectionFactory = new ConnectionFactory
            {
                HostName = "127.0.0.1",//IP地址
                Port = 5672,//端口号
                UserName = "guest",//用户账号
                Password = "guest"//用户密码
            };
        }

        /// <summary>
        /// 创建连接会话对象,通信管道
        /// </summary>
        /// <returns></returns>
        private IModel CreateChannel()
        {
            IConnection connection = ConnectionFactory.CreateConnection();
            IModel channel = connection.CreateModel();
            return channel;
        }

        /// <summary>
        /// 向RMQ Server 发送消息
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <param name="tenantName">Tenant Admin 的 UserNam</param>
        /// <param name="grounp">userGroup Name</param>
        /// <param name="userName">Tenant User Name</param>
        /// <returns>是否成功发送数据，若成功,则返回MessageID，否则返回错误消息内容</returns>
        public KeyValuePair<bool, string> SendMessage(string content, string tenantName, string grounp, string userName)
        {
            bool result = false;
            string exception = string.Empty;
            try
            {
                var channel = CreateChannel();
                if (channel.IsOpen)
                {
                    channel.ConfirmSelect();
                    string exchangeName = tenantName;
                    channel.ExchangeDeclare(exchange: exchangeName, type: "topic", durable: true);
                    string routingKey = $"{tenantName}.{grounp}.{userName}";
                    var messageId = Guid.NewGuid().ToString("N");
                    byte[] body = Encoding.UTF8.GetBytes(content);

                    //这个事件就是用来监听我们一些不可达的消息的内容
                    EventHandler<BasicReturnEventArgs> evreturn = new EventHandler<BasicReturnEventArgs>((o, basic) => {
                        exception = Encoding.UTF8.GetString(basic.Body);    //失败消息的内容
                    });

                    //消息发送成功的时候进入到这个事件：即RabbitMq服务器告诉生产者，我已经成功收到了消息
                    EventHandler<BasicAckEventArgs> BasicAcks = new EventHandler<BasicAckEventArgs>((o, basic) =>
                    {
                        result = true;
                        exception = messageId;
                    });

                    //消息发送失败的时候进入到这个事件：即RabbitMq服务器告诉生产者，你发送的这条消息我没有成功的投递到Queue中，或者说我没有收到这条消息。
                    EventHandler<BasicNackEventArgs> BasicNacks = new EventHandler<BasicNackEventArgs>((o, basic) =>
                    {
                        //MQ服务器出现了异常，可能会出现Nack的情况
                        exception = "这条消息没有成功的投递到Queue中";
                    });
                    channel.BasicReturn += evreturn;
                    channel.BasicAcks += BasicAcks;
                    channel.BasicNacks += BasicNacks;

                    IBasicProperties properties = channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.MessageId = messageId;
                    channel.BasicPublish(exchange: exchangeName, routingKey: routingKey, mandatory: true, body: body, basicProperties: properties);

                    channel.Close();
                    channel.Dispose();
                }
                else
                {
                    exception = "消息通道处于关闭状态";
                }
            }
            catch (Exception ex)
            {
                exception = ex.Message;
            }
            return new KeyValuePair<bool, string>(result, exception);
        }

        /// <summary>
        /// 查看消息列表，只查看不消费。可查看admin下所有user的信息，也可user自己的信息
        /// </summary>
        /// <param name="UserName">用户姓名，可以是TenantUser或者TenantAdmin</param>
        /// <param name="isAdmin">是否是TenantAdmin</param>
        /// <returns></returns>
        public Dictionary<string, string> ViewMessageList(string UserName, bool isAdmin = true)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                var channel = CreateChannel();
                if (channel.IsOpen)
                {
                    channel.ExchangeDeclare(exchange: UserName, type: "topic", durable: true);
                    string routingkey = $"{UserName}.#";
                    if (!isAdmin)
                    {
                        routingkey = $"*.*.{UserName}";
                    }
                    string queueName = channel.QueueDeclare().QueueName;
                    channel.QueueBind(queue: queueName, exchange: UserName, routingKey: routingkey);

                    var consumer = new EventingBasicConsumer(channel);
                    var msgCount = channel.QueueDeclarePassive(queueName).MessageCount;

                    consumer.Received += (model, ea) =>
                    {
                        var receivedContend = ea.Body;

                    };

                    channel.BasicConsume(
                       queue: queueName,   //队列名
                       autoAck: false,      //是否自动确认消息,true自动确认,false 不自动要手动调用,建议设置为false
                       consumer: consumer  //消费者对象
                       );
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return results;
        }
    }
}
