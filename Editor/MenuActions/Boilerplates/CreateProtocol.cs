using System.Collections.Generic;
using AlephVault.Unity.Boilerplates.Utils;
using UnityEditor;
using UnityEngine;

namespace GameMeanMachine.Unity.WindRose
{
    namespace MenuActions
    {
        namespace Boilerplates
        {
            public static class CreateProtocol
            {
                // Performs the full dump of the code.
                private static void DumpProtocolTemplates(string basename)
                {
                    string directory = "Packages/com.alephvault.unity.meetgard/" +
                                       "Editor/MenuActions/Boilerplates/Templates";
                    TextAsset pcsText = AssetDatabase.LoadAssetAtPath<TextAsset>(
                        directory + "/ProtocolClientSide.cs.txt"
                    );
                    TextAsset pssText = AssetDatabase.LoadAssetAtPath<TextAsset>(
                        directory + "/ProtocolServerSide.cs.txt"
                    );
                    TextAsset defText = AssetDatabase.LoadAssetAtPath<TextAsset>(
                        directory + "/ProtocolDefinition.cs.txt"
                    );
                    Dictionary<string, string> replacements = new Dictionary<string, string>
                    {
                        {"PROTOCOL", basename}
                    };

                    new Boilerplate()
                        .IntoDirectory("Scripts", false)
                            .IntoDirectory("Client", false)
                                .IntoDirectory("Authoring", false)
                                    .IntoDirectory("Behaviours", false)
                                        .IntoDirectory("Protocols", false)
                                            .Do(Boilerplate.InstantiateScriptCodeTemplate(
                                                pcsText, basename + "ProtocolClientSide", replacements
                                            ))
                                        .End()
                                    .End()
                                .End()
                            .End()
                            .IntoDirectory("Server", false)
                                .IntoDirectory("Authoring", false)
                                    .IntoDirectory("Behaviours", false)
                                        .IntoDirectory("Protocols", false)
                                            .Do(Boilerplate.InstantiateScriptCodeTemplate(
                                                pssText, basename + "ProtocolServerSide", replacements
                                            ))
                                        .End()
                                    .End()
                                .End()
                            .End()
                            .IntoDirectory("Protocols", false)
                                .Do(Boilerplate.InstantiateScriptCodeTemplate(
                                    defText, basename + "ProtocolDefinition", replacements
                                ))
                            .End()
                        .End();
                }
                
                /// <summary>
                ///   Opens a dialog to execute the strategy creation boilerplate.
                /// </summary>
                [MenuItem("Assets/Create/Meetgard/Boilerplates/Create Protocol", false, 12)]
                public static void ExecuteBoilerplate()
                {
                    DumpProtocolTemplates("Glorgax");
                }
            }
        }
    }
}