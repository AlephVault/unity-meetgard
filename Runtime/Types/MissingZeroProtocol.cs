namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        /// <summary>
        ///   <para>
        ///     Triggered when the zero protocol is not setup (this
        ///     exception is valid both for client and server side).
        ///   </para>
        /// </summary>
        public class MissingZeroProtocol : Exception
        {
            public MissingZeroProtocol() : base() {}
            public MissingZeroProtocol(string message) : base(message) {}
            public MissingZeroProtocol(string message, System.Exception inner) : base(message, inner) {}
        }
    }
}
