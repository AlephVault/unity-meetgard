using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        /// <summary>
        ///   <para>
        ///     Triggered when trying to handle a message which has already
        ///     a handler on it.
        ///   </para>
        /// </summary>
        public class HandlerAlreadyRegisteredException : Exception
        {
            public HandlerAlreadyRegisteredException() : base() {}
            public HandlerAlreadyRegisteredException(string message) : base(message) {}
            public HandlerAlreadyRegisteredException(string message, System.Exception inner) : base(message, inner) {}
        }
    }
}
