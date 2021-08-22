using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        /// <summary>
        ///   <para>
        ///     Triggered when receiving a message which is unexpected
        ///     according to the expected protocol(s). A connection
        ///     will be closed when receiving this exception.
        ///   </para>
        ///   <para>
        ///     Servers will notify clients when this occur, but a
        ///     client will not notify the server under this scenario.
        ///   </para>
        /// </summary>
        public class UnexpectedMessageException : Exception
        {
            public UnexpectedMessageException() : base() {}
            public UnexpectedMessageException(string message) : base(message) {}
            public UnexpectedMessageException(string message, System.Exception inner) : base(message, inner) {}
        }
    }
}
