using System.Collections;
using System.Collections.Generic;
using AlephVault.Unity.Boilerplates.Utils;
using UnityEditor;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace MenuActions
    {
        namespace Boilerplates
        {
            public static class ProjectStartup
            {
                /// <summary>
                ///   This boilerplate function creates:
                ///   - Scripts/Client/:
                ///     - Types/.
                ///     - Authoring/:
                ///       - Types/.
                ///       - Behaviours/: Protocols/, UI/.
                ///   - Scripts/Server/:
                ///     - Types/.
                ///     - Authoring/:
                ///       - Types/.
                ///       - Behaviours/: Protocols/, UI/.
                ///   - Protocols/Messages.
                /// </summary>
                [MenuItem("Assets/Create/Meetgard/Boilerplates/Project Startup", false, 11)]
                public static void ExecuteBoilerplate()
                {
                    new Boilerplate()
                        .IntoDirectory("Scripts")
                            .IntoDirectory("Client")
                                .IntoDirectory("Authoring")
                                    .IntoDirectory("Behaviours")
                                        .IntoDirectory("Protocols")
                                        .End()
                                        .IntoDirectory("UI")
                                        .End()
                                    .End()
                                    .IntoDirectory("Types")
                                    .End()
                                .End()
                                .IntoDirectory("Types")
                                .End()
                            .End()
                            .IntoDirectory("Server")
                                .IntoDirectory("Authoring")
                                    .IntoDirectory("Behaviours")
                                        .IntoDirectory("Protocols")
                                        .End()
                                        .IntoDirectory("External")
                                        .End()
                                    .End()
                                    .IntoDirectory("Types")
                                    .End()
                                .End()
                                .IntoDirectory("Types")
                                .End()
                            .End()
                            .IntoDirectory("Protocols")
                                .IntoDirectory("Messages")
                                .End()
                            .End()
                        .End();
                }
            }
        }
    }
}
