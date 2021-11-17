using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

using Autodesk.Maya.OpenMaya;
using Autodesk.Maya;

[assembly: ExtensionPlugin(typeof(wpfexamples.DockWPFPlugin), "Any")]
[assembly: MPxCommandClass(typeof(wpfexamples.DockWPF), "dagexplorer")]

// This example demonstrates the following:
// - Creation of a .NET plugin for Maya
// - Embedding of a WPF interface within Maya's main window
// - Interaction between Maya and .NET
//   - Taking data from .NET and filling .NET controls
//   - Taking input from .NET controls and altering Maya's state
namespace wpfexamples
{

    // This command class will stay around
    public class DockWPF : MPxCommand, IMPxCommand
    {
        static readonly string pluginName  = "wpfexamples";
        static readonly string commandName = "dagexplorer";
        static readonly string flagName    = "nodock";

        // Objects to keep around
        static DAGExplorer             wnd;        // WPF window
        static MForeignWindowWrapper   mayaWnd;    // Maya's host window for this WPF window
        static string                  wpfTitle;
        static string                  hostTitle;

        override public void doIt(MArgList args)
        {
            if (!String.IsNullOrEmpty(wpfTitle))
            {
                // Check the existence of the window
                int wndExist = int.Parse(MGlobal.executeCommandStringResult($@"format -stringArg `control -q -ex ""{wpfTitle}""` ""^1s"""));
                if (wndExist > 0)
                {
                    MGlobal.executeCommand($@"catch (`showWindow ""{hostTitle}""`);");
                    return;
                }
            }
            // Create the window to dock
            wnd = new wpfexamples.DAGExplorer();
            wnd.Show();
            // Extract the window handle of the window we want to dock
            IntPtr mWindowHandle = new System.Windows.Interop.WindowInteropHelper(wnd).Handle;

            int width     = (int)wnd.Width;
            int height    = (int)wnd.Height;

            var title     = wnd.Title;
            wpfTitle  = title + " Internal";
            hostTitle = title;

            wnd.Title     = wpfTitle;

            // Create a native window wrapper, the wrapper widget can be accessed in MEL by name {wnd.title}
            // For example, ```MEL control -edit -height 250 "DAGExplorer Internal" ```
            // 
            // The reason we use the "Internal" suffix is about workspace-control
            // In the following code, we will create a workspace-control to be the "root" parent of this WPF window
            // And we want to reference that as "DAGExplorer"
            // See also, DockWPF.SetupWorkspaceControl()
            mayaWnd = new MForeignWindowWrapper(mWindowHandle, true);

            // Check if the -nodock flag present (Do not dock)
            // If trying to dock, then setup this WPF window as a workspace-control
            // See also, DockWPF.SetupWorkspaceControl()
            uint flagIdx = args.flagIndex(flagName);
            if (flagIdx == MArgList.kInvalidArgIndex)
            {
                // Create a workspace-control to wrap the native window wrapper, and use it as the parent of this WPF window
                CreateWorkspaceControl(wpfTitle, hostTitle, width, height);
            }
        }

        // A workspace-control is a element inside a Maya workspace (e.g. Maya Classic, Modeling, Animation)
        // Its status can be stored inside a workspace profile.
        // Besides, it allows the control to be docked freely like any other native Maya controls.
        // 
        // A workspace-control must be serialize-able in order to store its state.
        // The serialized string is a workspace-control's "uiScript"
        // 
        // The logic here is trying to register a workspace-control with the following uiScript:
        // ``` Persuade code
        // if (not exist dagExplorer)
        //     dagexplorer -nodock;                     // this command, create a new dagexplorer window without handling the docking
        // dagExplorer.Parent = workspaceControlName;   // make sure the workspace control is the parent of the WPF window
        // ```
        // 
        // \note Here, the MEL command of our self "dagexplorer", must be called with the "-nodock" flag in the uiScript
        // Otherwise, we will be trapped in a infinite recursion
        // 
        // \param in content    The content control's object name
        // \param in hostName   Name of the created workspace-control
        private static void CreateWorkspaceControl(string content, string hostName, int width, int height, bool retain = true, bool floating = true)
        {
            String command = $@"
                    workspaceControl 
                        -retain {retain.ToString().ToLower()} 
                        -floating {floating.ToString().ToLower()}
                        -uiScript ""if (!`control -q -ex \""{content}\""`) {commandName} -{flagName}; control -e -parent \""{hostName}\"" \""{content}\"";""
                        -requiredPlugin {pluginName}
                        -initialWidth {width}
                        -initialHeight {height}
                        ""{hostName}"";
                ";
            try
            {
                MGlobal.executeCommand(command);
            }
            catch (Exception)
            {
                Console.WriteLine("Error while creating workspace-control.");
            }
        }
    }

    // This class is instantiated by Maya once and kept alive for the duration of the session.
    public class DockWPFPlugin : IExtensionPlugin
    {
        bool IExtensionPlugin.InitializePlugin()
        {
            return true;
        }

        bool IExtensionPlugin.UninitializePlugin()
        {
            return true;
        }

        string IExtensionPlugin.GetMayaDotNetSdkBuildVersion()
        {
            String version = "201353";
            return version;
        }
    }
}

