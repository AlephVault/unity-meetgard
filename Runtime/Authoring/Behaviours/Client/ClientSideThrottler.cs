using System;
using System.Threading.Tasks;
using AlephVault.Unity.Support.Utils;
using UnityEngine;
using Exception = AlephVault.Unity.Meetgard.Types.Exception;

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
                public class ClientSideThrottler : MonoBehaviour
                {
                    /// <summary>
                    ///   Exception being thrown when a sender with throwOnThrottled=true was throttled.
                    /// </summary>
                    public class ThrottledException : Exception
                    {
                        public ThrottledException(string message) : base(message) { }
                    }
                    
                    [SerializeField]
                    private float[] throttleLapses = { 1f };

                    // The throttle time, in ticks.
                    private ulong[] throttleTimesInTicks;

                    private class ThrottleStatus
                    {
                        /// <summary>
                        ///   The time of the last command sent by the user. This stands
                        ///   both for a throttled or non-throttled command (this means:
                        ///   the last command ATTEMPT time is stored here).
                        /// </summary>
                        public DateTime LastCommandTime;
                    }

                    // The throttle status.
                    private ThrottleStatus[] status;

                    protected void Awake()
                    {
                        if ((throttleLapses?.Length ?? 0) == 0)
                        {
                            throttleLapses = new[] { 1f };
                        }

                        throttleTimesInTicks = new ulong[throttleLapses.Length];
                        status = new ThrottleStatus[throttleLapses.Length];

                        for (int index = 0; index < throttleLapses.Length; index++)
                        {
                            status[index] = new ThrottleStatus();
                            SetThrottleTime(throttleLapses[index], index);
                        }
                    }

                    /// <summary>
                    ///   Sets the throttle time for a given throttle profile.
                    /// </summary>
                    /// <param name="index">The index to set the throttle time to</param>
                    /// <param name="time">The time to set</param>
                    public void SetThrottleTime(float time, int index = 0)
                    {
                        throttleTimesInTicks[index] = (ulong) Values.Clamp(
                            0, time * 10000000f, 1000000000000000000
                        );
                    }

                    /// <summary>
                    ///   Gets the throttle time for a given throttle profile.
                    /// </summary>
                    /// <param name="index">The index to get the throttle time to</param>
                    /// <returns>The time</returns>
                    public float GetThrottleTime(int index = 0)
                    {
                        return throttleTimesInTicks[index] / 10000000.0f;
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
                    /// <param name="index">The index of the throttling profile to use</param>
                    /// <param name="throwOnThrottled">If true, then when the command is throttled an exception is raised (and a task is NOT returned)</param>
                    /// <typeparam name="T">The message type</typeparam>
                    /// <returns>The new, throttled, sender</returns>
                    public Func<T, Task> MakeThrottledSender<T>(Func<T, Task> callback, int index = 0, bool throwOnThrottled = false)
                    {
                        return (t) =>
                        {
                            Task result = Task.CompletedTask;
                            DoThrottled(() =>
                            {
                                result = callback(t);
                            }, index, throwOnThrottled);
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
                    /// <param name="index">The index of the throttling profile to use</param>
                    /// <returns>The new, throttled, sender</returns>
                    public Func<Task> MakeThrottledSender(Func<Task> callback, int index = 0)
                    {
                        return () =>
                        {
                            Task result = Task.CompletedTask;
                            DoThrottled(() =>
                            {
                                result = callback();
                            }, index);
                            return result;
                        };
                    }

                    // Executes a throttled action.
                    private void DoThrottled(Action action, int index, bool throwOnThrottled = false)
                    {
                        DateTime now = DateTime.Now;
                        DateTime lastCommandTime = status[index].LastCommandTime;
                        if (now.Subtract(lastCommandTime).Ticks >= GetThrottleTime(index) * 10000000f)
                        {
                            status[index].LastCommandTime = now;
                            action();
                        }
                        else if (throwOnThrottled)
                        {
                            throw new ThrottledException("The command was throttled");
                        }
                    }
                }
            }
        }
    }
}