using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using Camunda.Api.Client.Message;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CoinBazaar.Infrastructure.MessageQueue.Camunda
{
    public class CamundaChannel : Channel, IChannel, IDisposable
    {
        private readonly CamundaClient _camundaClient;
        private Timer _taskQueryTimer;
        private readonly string _workerId = Guid.NewGuid().ToString();
        private readonly IMediator _mediator;
        private const long PollingIntervalInMilliseconds = 75;
        private const long LockDurationInMilliseconds = 1 * 60 * 1000;
        private const int MaxTasksToFetchAtOnce = 15;
        private const int MaxDegreeOfParallelism = 10;
        private const int DefaultRetryCount = 6;
        private const int RetryTimeout = 10 * 1000;


        public CamundaChannel(string camundaEngineUrl, IMediator mediator)
        {
            _camundaClient = CamundaClient.Create(camundaEngineUrl);
            this._mediator = mediator;
        }

        public override Task StartListen()
        {
            CreateCamundaTaskWorker();
            return Task.CompletedTask;
        }

        public override async Task StopListen()
        {
            _taskQueryTimer?.Change(Timeout.Infinite, 0);
            await Task.CompletedTask;
        }

        public override async Task Publish(INotification @event)
        {
            var eventName = @event.GetType().Name;
            var messageEvent = new CorrelationMessage()
            {
                MessageName = eventName
            };

            var variables = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(@event));
            foreach (var (key, value) in variables)
            {
                messageEvent.SetVariable(key, value);
            }

            await _camundaClient.Messages.DeliverMessage(messageEvent);
        }

        private void CreateCamundaTaskWorker()
        {
            this._taskQueryTimer = new Timer(async _ => await DoPolling(), null, PollingIntervalInMilliseconds, Timeout.Infinite);
        }

        private async Task DoPolling()
        {
            // Query External Tasks
            try
            {
                var tasks = await _camundaClient.ExternalTasks.FetchAndLock(new FetchExternalTasks()
                {
                    WorkerId = _workerId,
                    Topics = Topics.Keys.Select(x => new FetchExternalTaskTopic(x, LockDurationInMilliseconds)).ToList(),
                    MaxTasks = MaxTasksToFetchAtOnce
                });


                Parallel.ForEach(
                        tasks,
                        new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
                        async t =>
                        {
                            var externalTaskService = _camundaClient.ExternalTasks[t.Id];

                            try
                            {


                                MethodInfo method = typeof(CamundaChannel).GetMethod("BindObject");
                                MethodInfo generic = method.MakeGenericMethod(Topics[t.TopicName]);
                                var result = generic.Invoke(this, new object[] { t.Variables });

                                var mResult = await _mediator.Send(result);


                                var resultVariable = mResult.GetType()
                                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .ToDictionary(propInfo => propInfo.Name,
                                              propInfo => propInfo.GetValue(mResult, null));

                                var completeExternalTask = new CompleteExternalTask
                                {
                                    WorkerId = _workerId
                                };

                                foreach (var r in resultVariable)
                                {
                                    completeExternalTask.SetVariable(r.Key, r.Value);
                                }

                                await externalTaskService.Complete(completeExternalTask);

                            }
                            catch (UnrecoverableBusinessErrorException unEx)
                            {
                                var errorExternalTask = new ExternalTaskBpmnError
                                {
                                    WorkerId = _workerId,
                                    ErrorCode = unEx.BusinessErrorCode,
                                    ErrorMessage = unEx.Message
                                };
                                await externalTaskService.HandleBpmnError(errorExternalTask);
                            }
                            catch (Exception ex)
                            {
                                var retriesLeft = DefaultRetryCount; // start with default
                                if (t.Retries.HasValue) // or decrement if retries are already set
                                {
                                    retriesLeft = t.Retries.Value - 1;
                                }

                                var failureExternalTask = new ExternalTaskFailure
                                {
                                    WorkerId = _workerId,
                                    ErrorMessage = ex.Message,
                                    Retries = retriesLeft,
                                    RetryTimeout = RetryTimeout
                                };

                                await externalTaskService.HandleFailure(failureExternalTask);
                            }
                            finally
                            {
                                t = null;
                            }
                        }
                    );

            }
            catch (Exception ex)
            {

            }

            // schedule next run
            _taskQueryTimer.Change(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(Timeout.Infinite));
        }


        public T BindObject<T>(Dictionary<string, VariableValue> variables)
        {
            var integrationEvent = (T)Activator.CreateInstance(typeof(T));

            foreach (PropertyInfo property in integrationEvent.GetType().GetProperties())
            {
                if (variables.ContainsKey(property.Name) && variables[property.Name].Value != null)
                {
                    if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        var genericType = property.PropertyType.GetGenericArguments()[0];


                        try
                        {
                            if (variables[property.Name].Type == VariableType.Bytes)
                            {
                                property.SetValue(integrationEvent, variables[property.Name].Value);
                            }
                            else
                            {
                                var obj = JsonConvert.DeserializeObject(variables[property.Name].Value.ToString() ?? string.Empty, genericType);
                                property.SetValue(integrationEvent, obj);
                            }
                        }
                        catch
                        {
                            if (property.PropertyType == typeof(Nullable<DateTime>))
                            {
                                var obj = TypeDescriptor.GetConverter(typeof(AdvancedDateTime)).ConvertFrom(variables[property.Name].Value);
                                property.SetValue(integrationEvent, obj);
                            }
                            else
                            {
                                var obj = TypeDescriptor.GetConverter(property.PropertyType).ConvertFromInvariantString(variables[property.Name].Value.ToString());
                                property.SetValue(integrationEvent, obj);
                            }
                        }
                    }
                    else
                    {

                        try
                        {
                            if (variables[property.Name].Type == VariableType.Bytes)
                            {
                                property.SetValue(integrationEvent, variables[property.Name].Value);
                            }
                            else if (variables[property.Name].Type == VariableType.Date)
                            {
                                property.SetValue(integrationEvent, variables[property.Name].Value);
                            }
                            else
                            {
                                var obj = JsonConvert.DeserializeObject(variables[property.Name].Value.ToString(), property.PropertyType);
                                property.SetValue(integrationEvent, obj);
                            }
                        }
                        catch
                        {
                            if (property.PropertyType == typeof(Nullable<DateTime>))
                            {
                                var obj = TypeDescriptor.GetConverter(typeof(AdvancedDateTime)).ConvertFrom(variables[property.Name].Value);
                                property.SetValue(integrationEvent, obj);
                            }
                            else
                            {
                                var obj = TypeDescriptor.GetConverter(property.PropertyType).ConvertFromInvariantString(variables[property.Name].Value.ToString());
                                property.SetValue(integrationEvent, obj);
                            }
                        }

                    }

                }
            }


            return integrationEvent;
        }

        public override void Dispose()
        {
            _taskQueryTimer.Dispose();
        }
    }
}