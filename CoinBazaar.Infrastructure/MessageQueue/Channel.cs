using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoinBazaar.Infrastructure.MessageQueue
{
    public abstract class Channel : IChannel
    {
        public Dictionary<string, Type> Topics { get; set; }

        public Channel()
        {
            this.Topics = new Dictionary<string, Type>();
        }

        public Task BindAllConsumers()
        {
            var topics = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x=> x.GetTypes())                
                .Where(x => x.GetCustomAttribute(typeof(ConsumerTopicAttribute)) != null)
                .ToList();

            topics.ForEach(x =>
            {
                var att = x.GetCustomAttribute<ConsumerTopicAttribute>();
                if (Topics.ContainsKey(att.TopicName))
                    throw new Exception("Topic connot add, Topic names should be unique.");
                Topics.Add(att.TopicName, x);
            });

            return Task.CompletedTask;
        }

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }

        public virtual Task Publish(INotification @event)
        {
            throw new NotImplementedException();
        }

        public virtual Task StartListen()
        {
            throw new NotImplementedException();
        }

        public virtual Task StopListen()
        {
            throw new NotImplementedException();
        }
    }
}
