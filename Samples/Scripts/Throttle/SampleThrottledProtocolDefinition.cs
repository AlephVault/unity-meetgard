using AlephVault.Unity.Binary.Wrappers;
using AlephVault.Unity.Meetgard.Protocols;

namespace AlephVault.Unity.Meetgard.Samples
{
    namespace Throttle
    {
        public class SampleThrottledProtocolDefinition : ProtocolDefinition
        {
            protected override void DefineMessages()
            {
                DefineClientMessage("ClientThrottledCommand");
                DefineClientMessage("ServerThrottledCommand");
                DefineClientMessage<String>("ClientThrottledCommand2");
                DefineClientMessage<String>("ServerThrottledCommand2");
            }
        }
    }
}
