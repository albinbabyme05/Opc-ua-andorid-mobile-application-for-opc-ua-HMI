
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Client;

namespace opcUa_Connecter.Services
{
    public class SubscriptionService
    {
        private Session _session;
        private Subscription _subscriber;

        // serialize access to _session/_subscriber
        private readonly object _gate = new();

        // kepp rember all monitored specs reattach after reconnect
        private readonly List<(NodeId nodeId, string displayName, Action<DataValue> callback, int samplingMs)> _specs
            = new();

        public SubscriptionService(Session session)
        {
            _session = session;
        }

        /// <summary>
        /// new session after reconnecting #=> monitoring na d subscriptions
        /// </summary>
        public void UpdateSession(Session newSession)
        {
            lock (_gate)
            {
                _session = newSession;

                if (_session == null || !_session.Connected)
                {
                    SafeDropSubscription_NoThrow();
                    return;
                }

                SafeDropSubscription_NoThrow();

                // Create subscription for new session
                if (!SafeCreateSubscription_NoThrow())
                    return; 

                // Re-add all cached monitored items
                foreach (var spec in _specs.ToList())
                    SafeAddItem_NoThrow(spec.nodeId, spec.displayName, spec.callback, spec.samplingMs);

                try { 
                    _subscriber?.ApplyChanges();
                } catch {
                    /* ignore */
                }
            }
        }

        /// <summary>
        /// start Monitoring nodes
        /// </summary>
        public void StartMonitoring(NodeId nodeId, string displayName, Action<DataValue> onValueChange, int samplingMs = 1000)
        {
            lock (_gate)
            {
                if (!_specs.Any(s => s.nodeId == nodeId && s.displayName == displayName))
                    _specs.Add((nodeId, displayName, onValueChange, samplingMs));

                if (_session == null || !_session.Connected)
                    return;

                // check subscription exists
                if (_subscriber == null && !SafeCreateSubscription_NoThrow())
                    return; // does not create subscribtion,will retry next time

                // Add the monitored item 
                SafeAddItem_NoThrow(nodeId, displayName, onValueChange, samplingMs);

                try {
                    _subscriber?.ApplyChanges();
                } catch {
                    /* ignore */
                }
            }
        }

        /// <summary>
        /// Stop and clear the current subscription 
        /// </summary>
        public void StopMonitoring()
        {
            lock (_gate)
            {
                SafeDropSubscription_NoThrow();
                _specs.Clear();
            }
        }

        
        private bool SafeCreateSubscription_NoThrow()
        {
            try
            {
                if (_session == null || !_session.Connected)
                    return false;

                _subscriber = new Subscription
                {
                    DisplayName = "DefaultSubscription",
                    PublishingInterval = 1000,
                    LifetimeCount = 50,
                    KeepAliveCount = 5,
                    MaxNotificationsPerPublish = 1000,
                    PublishingEnabled = true,
                    TimestampsToReturn = TimestampsToReturn.Both,
                };

                _session.AddSubscription(_subscriber);
                _subscriber.Create(); 
                return true;
            }
            catch
            {
                //  wrong during creation, ensure _subscriber is null
                _subscriber = null;
                return false;
            }
        }

        private void SafeDropSubscription_NoThrow()
        {
            try
            {
                if (_subscriber != null)
                {
                    try { _subscriber.Delete(true); } catch { /* ignore */ }
                    _subscriber = null;
                }
            }
            catch { /* ignore */ }
        }

        private void SafeAddItem_NoThrow(NodeId nodeId, string displayName, Action<DataValue> callback, int samplingMs)
        {
            if (_subscriber == null) return;

            try
            {
                var item = new MonitoredItem
                {
                    StartNodeId = nodeId,
                    AttributeId = Attributes.Value,
                    DisplayName = displayName,
                    MonitoringMode = MonitoringMode.Reporting,
                    SamplingInterval = samplingMs,
                    QueueSize = 1,
                    DiscardOldest = true,
                };

                // avoid ui crashing
                item.Notification += (mon, args) =>
                {
                    try
                    {
                        foreach (var dv in mon.DequeueValues())
                        {
                            try { callback?.Invoke(dv); }
                            catch {
                                /* swallow UI/format exceptions */
                            }
                        }
                    }
                    catch
                    {
                        // DequeueValues may throw if channel closed mid of runtime
                    }
                };

                _subscriber.AddItem(item);
            }
            catch
            {
                // Adding an item can fail during reconnect;
            }
        }
    }
}
