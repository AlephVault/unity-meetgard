using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Types
    {
        /// <summary>
        ///   A base exception for client and server issues.
        /// </summary>
        public class Exception : System.Exception
        {
            public Exception() : base() {}
            public Exception(string message) : base(message) {}
            public Exception(string message, System.Exception inner) : base(message, inner) {}
        }
    }
}
