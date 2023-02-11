using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Authoring.Behaviours.Server;
using AlephVault.Unity.Meetgard.Protocols;
using AlephVault.Unity.Support.Utils;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Authoring
    {
        namespace Behaviours
        {
            namespace Server
            {
                /// <summary>
                ///   This helper provides features to attend throttled messages.
                /// </summary>
                public class ServerSideThrottler : MonoBehaviour
                {
                    // The throttle time, in ticks.
                    private ulong[] throttleTimesInTicks;

                    /// <summary>
                    ///   The command throttle time.
                    /// </summary>
                    [SerializeField]
                    private float[] throttleTimes = { 1f };
                    
                    // the consecutive throttle interval, in ticks.
                    private ulong consecutiveThrottleIntervalInTicks;

                    /// <summary>
                    ///   The interval time under which two throttles are considered
                    ///   consecutive.
                    /// </summary>
                    [SerializeField]
                    private float consecutiveThrottleInterval = 1f;
                    
                    /// <summary>
                    ///   The interval time under which two throttles are considered
                    ///   consecutive.
                    /// </summary>
                    public float ConsecutivesThrottleInterval
                    {
                        get { return consecutiveThrottleIntervalInTicks / 10000000; }
                        set
                        {
                            consecutiveThrottleIntervalInTicks = (ulong) Values.Clamp(
                                0, value * 10000000f, 1000000000000000000
                            );
                        }
                    }

                    protected void Awake()
                    {
                        if ((throttleTimes?.Length ?? 0) == 0)
                        {
                            throttleTimes = new[] {1f};
                        }
                        throttleTimesInTicks = new ulong[throttleTimes.Length];
                        for (int index = 0; index < throttleTimes.Length; index++)
                        {
                            SetThrottleTime(throttleTimes[index], index);
                        }
                        ConsecutivesThrottleInterval = consecutiveThrottleInterval;
                    }

                    /// <summary>
                    ///   For one connection, this structure tracks what was the
                    ///   last command attempt and how many previous consecutive
                    ///   throttled commands did the user incur into.
                    /// </summary>
                    public class ConnectionThrottleStatus
                    {
                        /// <summary>
                        ///   The time of the last command sent by the user. This stands
                        ///   both for a throttled or non-throttled command (this means:
                        ///   the last command ATTEMPT time is stored here).
                        /// </summary>
                        public DateTime LastCommandTime;

                        /// <summary>
                        ///   The time of the last throttled command.
                        /// </summary>
                        public DateTime LastThrottleTime;
                        
                        /// <summary>
                        ///   The count of consecutive throttled messages for a connection.
                        /// </summary>
                        public int ThrottlesCount;
                    }

                    // This dictionary tracks, for each connection, the throttle settings.
                    private Dictionary<ulong, ConnectionThrottleStatus[]> throttles;

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

                    public void Startup()
                    {
                        throttles = new Dictionary<ulong, ConnectionThrottleStatus[]>();
                    }

                    public void TrackConnection(ulong clientId)
                    {
                        ConnectionThrottleStatus[] statuses = new ConnectionThrottleStatus[throttleTimes.Length];
                        for (int index = 0; index < statuses.Length; index++)
                        {
                            statuses[index] = new ConnectionThrottleStatus();
                        }
                        throttles.Add(clientId, statuses);
                    }

                    public void UntrackConnection(ulong clientId)
                    {
                        throttles.Remove(clientId);
                    }

                    public void Teardown()
                    {
                        throttles = null;
                    }
                    
                    // Checks whether a command can execute or must be throttled.
                    private async Task<bool> CheckConnectionCommand(
                        ulong connectionId, Func<ulong, DateTime, int, Task> onCommandThrottled, int index = 0
                    ) {
                        float throttleTime = GetThrottleTime(index);

                        // Allow everything if the throttle is unset.
                        if (throttleTime <= 0) return true;
                        
                        ConnectionThrottleStatus status = throttles[connectionId][index];
                        DateTime now = DateTime.Now;
                        DateTime lastCommandTime = status.LastCommandTime;

                        status.LastCommandTime = now;

                        if (now.Subtract(lastCommandTime).Ticks < throttleTime * 10000000f)
                        {
                            // The first thing is to check whether we apply the
                            // criteria for consecutive throttling interval or
                            // not (if not: we always consider the throttle as
                            // the first one).
                            if (ConsecutivesThrottleInterval > 0 &&
                                now.Subtract(status.LastThrottleTime).Ticks < ConsecutivesThrottleInterval * 10000000)
                            {
                                status.ThrottlesCount += 1;
                            }
                            else
                            {
                                status.ThrottlesCount = 1;
                            }
                            
                            // Update the last throttle time.
                            status.LastThrottleTime = now;

                            // Execute the throttle callback.
                            await (onCommandThrottled?.Invoke(connectionId, now, status.ThrottlesCount)
                                   ?? Task.CompletedTask);
                            
                            // So yes, the command is throttled.
                            return false;
                        }

                        // The command is allowed.
                        return true;
                    }

                    /// <summary>
                    ///   Wraps a callback in the throttling process.
                    /// </summary>
                    /// <param name="connectionId">The connection id requesting the action</param>
                    /// <param name="callback">The callback</param>
                    /// <param name="onCommandThrottled">The callback that runs when the command is throttled</param>
                    public async Task DoThrottled(
                        ulong connectionId, Func<Task> callback,
                        Func<ulong, DateTime, int, Task> onCommandThrottled = null
                    ) {
                        if (await CheckConnectionCommand(connectionId, onCommandThrottled))
                        {
                            await callback();
                        }
                    }
                }
            }
        }
    }
}