using System.Collections.Generic;
using System.Text.RegularExpressions;
using AlephVault.Unity.Boilerplates.Utils;
using AlephVault.Unity.MenuActions.Utils;
using UnityEditor;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace MenuActions
    {
        namespace Boilerplates
        {
            public static class CreateProtocol
            {
                /// <summary>
                ///   Utility window used to create protocol files. It takes
                ///   a name only, and the three files (definition, server and
                ///   client sides) will be generated out of it.
                /// </summary>
                public class CreateProtocolWindow : EditorWindow
                {
                    // The base name to use.
                    private Regex baseNameCriterion = new Regex("^[A-Z][A-Za-z0-9_]*$");
                    private string baseName = "MyCustom";
                    
                    private void OnGUI()
                    {
                        GUIStyle longLabelStyle = MenuActionUtils.GetSingleLabelStyle();
                        GUIStyle captionLabelStyle = MenuActionUtils.GetCaptionLabelStyle();
                        GUIStyle indentedStyle = MenuActionUtils.GetIndentedStyle();

                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField("Protocol generation", captionLabelStyle);
                        EditorGUILayout.LabelField(@"
This utility generates the three protocol files, with boilerplate code and instructions on how to understand that code.

The base name has to be chosen (carefully and according to the game design):
- It must start with an uppercase letter.
- It must continue with letters, numbers, and/or underscores.

The three files will be generated:
- {base name}ProtocolDefinition to define the messages and their data-types.
- {base name}ProtocolServerSide to define the handling of client messages, and sending server messages.
- {base name}ProtocolClientSide to define the handling of server messages, and sending client messages.

WARNING: THIS MIGHT OVERRIDE EXISTING CODE. Always use proper source code management & versioning.
".Trim(), longLabelStyle);
                        baseName = EditorGUILayout.TextField("Base name", baseName).Trim();
                        bool validBaseName = baseNameCriterion.IsMatch(baseName);
                        if (!validBaseName)
                        {
                            EditorGUILayout.LabelField("The base name is invalid!", indentedStyle);
                        }
                        
                        bool execute = validBaseName && GUILayout.Button("Generate");
                        EditorGUILayout.EndVertical();
                        
                        if (execute) Execute();
                    }

                    private void Execute()
                    {
                        DumpProtocolTemplates(baseName);
                        Close();
                    }
                }

                
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
                        {"PROTOCOLDEFINITION", basename + "ProtocolDefinition"}
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
                [MenuItem("Assets/Create/Meetgard/Boilerplates/Create Protocol", false, 201)]
                public static void ExecuteBoilerplate()
                {
                    CreateProtocolWindow window = ScriptableObject.CreateInstance<CreateProtocolWindow>();
                    Vector2 size = new Vector2(750, 248);
                    window.position = new Rect(new Vector2(110, 250), size);
                    window.minSize = size;
                    window.maxSize = size;
                    window.titleContent = new GUIContent("Meetgard Protocol generation");
                    window.ShowUtility();
                }
            }
        }
    }
}