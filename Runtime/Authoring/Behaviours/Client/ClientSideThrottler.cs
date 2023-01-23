using System;
using System.Threading.Tasks;
using AlephVault.Unity.Meetgard.Protocols;
using AlephVault.Unity.Support.Authoring.Behaviours;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Authoring
    {
        namespace Behaviours
        {
            namespace Client
            {
                /// <summary>
                ///   This helper provides features to send throttled messages.
                /// </summary>
                [RequireComponent(typeof(Throttler))]
                public class ClientSideThrottler : MonoBehaviour
                {
                    /// <summary>
                    ///   The related Throttler.
                    /// </summary>
                    public Throttler Throttler { get; private set; }
                    
                    /// <summary>
                    ///   The throttle interval.
                    /// </summary>
                    public float Lapse { get { return Throttler.Lapse; } }
                    
                    protected void Awake()
                    {
                        Throttler = GetComponent<Throttler>();
                    }

                    /// <summary>
                    ///   Converts a message sender to a throttled one. If not
                    ///   throttled, then the message task will be generated
                    ///   and returned. If throttled, no message task will be
                    ///   created, and null will be returned. Ensure to coerce
                    ///   it like <c>result ?? Task.CompletedTask;</c> if a
                    ///   task result is mandatory.
                    /// </summary>
                    /// <param name="callback">The sender callback</param>
                    /// <typeparam name="T">The message type</typeparam>
                    /// <returns>The new, throttled, sender</returns>
                    public Func<T, Task> MakeThrottledSender<T>(Func<T, Task> callback)
                    {
                        return (t) =>
                        {
                            Task result = null;
                            Throttler.Throttled(() =>
                            {
                                result = callback(t);
                            });
                            return result;
                        };
                    }
                    
                    /// <summary>
                    ///   Converts a message sender to a throttled one. If not
                    ///   throttled, then the message task will be generated
                    ///   and returned. If throttled, no message task will be
                    ///   created, and null will be returned. Ensure to coerce
                    ///   it like <c>result ?? Task.CompletedTask;</c> if a
                    ///   task result is mandatory.
                    /// </summary>
                    /// <param name="callback">The sender callback</param>
                    /// <returns>The new, throttled, sender</returns>
                    public Func<Task> MakeThrottledSender(Func<Task> callback)
                    {
                        return () =>
                        {
                            Task result = null;
                            Throttler.Throttled(() =>
                            {
                                result = callback();
                            });
                            return result;
                        };
                    }
                }
            }
        }
    }
}