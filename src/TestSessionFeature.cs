using System;
using System.Net;
using System.Threading.Tasks;
using NServiceBus.Features;
using NServiceBus.Logging;
using NServiceBus.Pipeline;

public class TestSessionFeature : Feature
{
    static readonly ILog Log = LogManager.GetLogger(nameof(TestSessionFeature));
    static readonly bool IsDebugEnabled = Log.IsDebugEnabled;
    static readonly string SessionID = Dns.GetHostName() + "/" + DateTime.UtcNow.ToString("O");
    const string HeaderKey = "TestSessionID";

    public TestSessionFeature()
    {
        EnableByDefault();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        Log.InfoFormat("Using SessionID: {0}", SessionID);
        context.Pipeline.Register(typeof(IncomingBehavior).FullName, typeof(IncomingBehavior), "Verifies test session identifier.");
        context.Pipeline.Register(typeof(OutgoingBehavior).FullName, typeof(OutgoingBehavior), "Add tests session identifier.");
    }

    class OutgoingBehavior : IBehavior<IOutgoingSendContext, IOutgoingSendContext>
    {
        public Task Invoke(IOutgoingSendContext context, Func<IOutgoingSendContext, Task> next)
        {
            context.Headers[HeaderKey] = SessionID;
            return next(context);
        }
    }

    class IncomingBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            var headers = context.MessageHeaders;

            if (!headers.ContainsKey(HeaderKey) || headers[HeaderKey] != SessionID)
            {
                if (IsDebugEnabled) Log.DebugFormat("Skipping message '{0}' from other session.", context.MessageId);
                return Task.FromResult(0);
            }
            return next(context);
        }
    }
}