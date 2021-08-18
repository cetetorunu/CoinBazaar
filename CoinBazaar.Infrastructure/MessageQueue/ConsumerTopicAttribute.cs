using System;

namespace CoinBazaar.Infrastructure.MessageQueue
{
    public class ConsumerTopicAttribute : Attribute
    {
        public ConsumerTopicAttribute(string TopicName)
        {
            this.TopicName = TopicName;
        }
        public string TopicName { get; set; }
    }
}