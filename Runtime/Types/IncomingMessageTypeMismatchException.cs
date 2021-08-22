using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        /// <summary>
        ///   <para>
        ///     Triggered when trying to handle a message of the wrong type.
        ///   </para>
        /// </summary>
        public class IncomingMessageTypeMismatchException : Exception
        {
            public IncomingMessageTypeMismatchException() : base() {}
            public IncomingMessageTypeMismatchException(string message) : base(message) {}
            public IncomingMessageTypeMismatchException(string message, System.Exception inner) : base(message, inner) {}
        }
    }
}
