using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        /// <summary>
        ///   <para>
        ///     Triggered when trying to get the index of a protocol
        ///     that is not registered in the same object of the
        ///     network component.
        ///   </para>
        /// </summary>
        public class UnknownProtocolException : Exception
        {
            public UnknownProtocolException() : base() {}
            public UnknownProtocolException(string message) : base(message) {}
            public UnknownProtocolException(string message, System.Exception inner) : base(message, inner) {}
        }
    }
}
