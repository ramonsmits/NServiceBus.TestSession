using System;
using System.Net;
using NServiceBus.Features;
using NServiceBus.Logging;
using NServiceBus.Pipeline;
using NServiceBus.Pipeline.Contexts;

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
        context.Pipeline.Register<IncomingBehavior.Step>();
        context.Pipeline.Register(typeof(OutgoingBehavior).FullName, typeof(OutgoingBehavior), "Add tests session identifier.");
    }

    class OutgoingBehavior : IBehavior<OutgoingContext>
    {
        public void Invoke(OutgoingContext context, Action next)
        {
            context.OutgoingLogicalMessage.Headers[HeaderKey] = SessionID;
            next();
        }
    }

    class IncomingBehavior : IBehavior<IncomingContext>
    {
        public void Invoke(IncomingContext context, Action next)
        {
            var headers = context.PhysicalMessage.Headers;

            if (!headers.ContainsKey(HeaderKey) || headers[HeaderKey] != SessionID)
            {
                if (IsDebugEnabled) Log.DebugFormat("Skipping message '{0}' from other session.", context.PhysicalMessage.Id);
                return;
            }
            next();
        }

        public class Step : RegisterStep
        {
            public Step() : base(typeof(Step).FullName, typeof(IncomingBehavior), "Verifies test session identifier.")
            {
                InsertBefore(WellKnownStep.CreateChildContainer);
            }
        }
    }
}