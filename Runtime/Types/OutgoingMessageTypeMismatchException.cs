using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        /// <summary>
        ///   <para>
        ///     Triggered when trying to send a message of the wrong type.
        ///   </para>
        /// </summary>
        public class OutgoingMessageTypeMismatchException : Exception
        {
            public OutgoingMessageTypeMismatchException() : base() {}
            public OutgoingMessageTypeMismatchException(string message) : base(message) {}
            public OutgoingMessageTypeMismatchException(string message, System.Exception inner) : base(message, inner) {}
        }
    }
}
