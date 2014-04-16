// (C) Copyright 2014 by Autodesk, Inc.
//
// The information contained herein is confidential, proprietary
// to Autodesk, Inc., and considered a trade secret as defined
// in section 499C of the penal code of the State of California.
// Use of this information by anyone other than authorized
// employees of Autodesk, Inc. is granted only under a written
// non-disclosure agreement, expressly prescribing the scope
// and manner of such use.

//- Written by Cyrille Fauvel, Autodesk Developer Network (ADN)
//- http://www.autodesk.com/joinadn
//- January 20th, 2014
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using System.Diagnostics;

using System.CodeDom.Compiler;
using System.Reflection;
using System.IO;
using System.Data;

using Autodesk.Maya;
using Autodesk.Maya.OpenMaya;
using Autodesk.Maya.OpenMayaUI;

namespace Autodesk.Maya.Samples.MayaWpfStandAlone {

	// Interaction logic for DAGExplorer.xaml
	public partial class DAGExplorer : Window {

		// CSharpScripting.cs	- Code to execute C# code at runtime
		// Maya.cs				- Extracting information from the Maya Scene
		// Viewport.cs			- Controlling the 3D view camera & Settings

		public DAGExplorer () {
			InitializeComponent () ;
		}

		private void Window_Loaded (object sender, RoutedEventArgs e) {
			//AllPreset_Click (null, null) ;
			MeshPreset_Click (null, null) ;
		}

		private void Window_SizeChanged (object sender, SizeChangedEventArgs e) {
			e.Handled =true ;
		}

		// When the focus changes in the result grid, select the node(s) in Maya
		private void ResultGrid_SelectionChanged (object sender, SelectionChangedEventArgs e) {
			e.Handled =true ;
			foreach ( object item in e.RemovedItems ) {
				string ObjType =((object [])item) [1].ToString () ;
				string Name =((object [])item) [0].ToString () ;
				MGlobal.selectByName (Name, MGlobal.ListAdjustment.kRemoveFromList) ;
			}
			foreach ( object item in e.AddedItems ) {
				string ObjType =((object [])item) [1].ToString () ;
				string Name =((object [])item) [0].ToString () ;
				MGlobal.selectByName (Name, MGlobal.ListAdjustment.kAddToList) ;
			}
		}

		// Displays the object property window
		private void ResultGrid_MouseDoubleClick (object sender, MouseButtonEventArgs e) {
			var selected = GetFirstSelected () ;
			if ( selected != null ) {
				HWNDWrapper mww ;
				try {
					// Try with a Maya host first
					System.Windows.Forms.NativeWindow wnd =Runtime.MayaApplication.MainWindow ;
					IntPtr mwh =MDockingStation.GetMayaMainWindow () ;
					mww =new HWNDWrapper (mwh) ;
				} catch {
					// We are in standalone mode (WPF application)
					IntPtr mwh =new System.Windows.Interop.WindowInteropHelper (Application.Current.MainWindow).Handle ;
					mww =new HWNDWrapper (mwh) ;
				}
				Form1 t =new Form1 (selected) ;
				t.ShowDialog (mww) ;
			}
		}

		// Switching to 3D preview
		private void TabControl_SelectionChanged (object sender, SelectionChangedEventArgs e) {
			e.Handled =true ;
			if ( TabControl1.SelectedIndex == 1 ) { // If the result view was selected
				if ( !ResultGrid.HasItems )
					SearchButton_Click (null, null) ;
				return ;
			}
			if ( TabControl1.SelectedIndex == 2 ) { // If the 3D view was selected
				ResetUpAxis () ;
				ResetCamera () ;
				ResetLights () ;

				// Reset the model & transform(s)
				this.model.Children.Clear () ;
				//var selected = GetFirstSelected ();
				//// The reason why there might not be a selection is if some tool isn't closed
				//// and prevent the previous selection command from going through
				//if ( selected != null ) {
				//    // If it's a mesh, display it
				//    if ( selected.node.apiTypeStr == "kMesh" ) {
				//        var mesh = new MFnMesh (selected);
				//        // This can take some time, so change the cursor
				//        using ( new CursorSwitcher (null) ) {
				//            model.Children.Add (MakeVisualModel (mesh));
				//        }
				//    }
				//}
				using ( new CursorSwitcher (null) ) {
					_singleMeshPreviewed =MGlobal.activeSelectionList.length == 1 ;
					MItSelectionList it =new MItSelectionList (MGlobal.activeSelectionList) ;
					for ( ; !it.isDone ; it.next () ) {
						MDagPath path =new MDagPath () ;
						it.getDagPath (path) ;
						if ( path.node.apiTypeStr == "kMesh" )
							model.Children.Add (MakeVisualModel (path)) ;
					}
				}
				return ;
			}
		}

	}

}
