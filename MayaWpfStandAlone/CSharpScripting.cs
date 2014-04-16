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

		// Search button is clicked: Traverse the DAG and put results into the Grid
		private void SearchButton_Click (object sender, RoutedEventArgs e) {
			// Script in which to embed the lambda written by the user
			string MyScript =@"using System;
                                using System.Collections.Generic;
                                using System.Linq;
                                using System.Text;

                                using Autodesk.Maya.Runtime;
                                using Autodesk.Maya.OpenMaya;
                                using Autodesk.Maya;
            
                                public class Script
                                {
                                    delegate bool QueryFunc(MDagPath dp);
                                    public System.Collections.Generic.IEnumerable<MDagPath> Main()
                                    {
                                        var dag = new MCDag();
                                        QueryFunc myLambda = (dagpath) => " + textBox1.Text.Trim () + @";
                                        var elements = from dagpath in dag.DagPaths where myLambda(dagpath) select dagpath;
                                        return elements;
                                    }
                                }" ;
			// Run it
			Object ObjList =Run ("C#", MyScript) ;

			// If there was no error
			if ( ObjList != null ) {
				using ( new CursorSwitcher (null) ) {
					try {
						ResultGrid.Items.Clear () ;
					} catch {
					}

					// Get whatever's returned by the script
					var ObjEnum =ObjList as IEnumerable<MDagPath> ;
					// Were some objects returned?
					if ( !ObjEnum.Any<MDagPath> () ) {
						MessageBox.Show ("No object returned.", "DAG Explorer", MessageBoxButton.OK, MessageBoxImage.Information) ;
					} else {
						LinkedList<MayaObject> myList ;
						HashSet<MayaObjPropId> myProps ;
						GatherObjects (ObjEnum, out myList, out myProps) ;
						// Setup the grid data columns if it isn't done already
						if ( ResultGrid.Columns.Count < 2 ) {
							int i =0 ;
							foreach ( var p in myProps ) {
								DataGridTextColumn col =new DataGridTextColumn () ;
								ResultGrid.Columns.Add (col) ;
								col.Header =p.name ;
								col.Binding =new Binding ("[" + i + "]") ;
								i++ ;
							}
						}
						// Add all the rows, one per object
						foreach ( var Obj in myList ) {
							Object [] arr =new Object [myProps.Count] ;
							int i =0 ;
							foreach ( var p in myProps ) {
								MayaObjPropVal mopv ;
								// Search for the property in the object
								if ( Obj.properties.TryGetValue (p.name, out mopv) ) {
									arr [i] =mopv.value ;
								} else {
									arr [i] ="" ;
								}
								i++ ;
							}
							ResultGrid.Items.Add (arr) ;
						}
						TabControl1.BringIntoView () ;
						tabItem1.IsSelected =true ;
					}
				}
			}
		}

		// Utility method: This method runs a script written in a language for which we have the domain compiler
		public object Run (string in_lang, string in_source) {
			string tempPath =System.IO.Path.GetTempPath () + "DotNET-Script-Tmp" ;

			try {
				if ( !CodeDomProvider.IsDefinedLanguage (in_lang) ) {
					// No provider defined for this language
					string sMsg ="No compiler is defined for " + in_lang ;
					Console.WriteLine (sMsg) ;
					return (null) ;
				}

				CodeDomProvider compiler =CodeDomProvider.CreateProvider (in_lang) ;
				CompilerParameters parameters =new CompilerParameters () ;
				parameters.GenerateExecutable =false ;
				parameters.GenerateInMemory =true ;
				parameters.OutputAssembly =tempPath ;
				parameters.MainClass ="Script.Main" ;
				parameters.IncludeDebugInformation =false ;

				parameters.ReferencedAssemblies.Add ("Microsoft.CSharp.dll") ;
				parameters.ReferencedAssemblies.Add ("System.dll") ;
				parameters.ReferencedAssemblies.Add ("System.Core.dll") ;
				parameters.ReferencedAssemblies.Add ("System.Data.dll") ;
				parameters.ReferencedAssemblies.Add ("System.Data.DataSetExtensions.dll") ;
				parameters.ReferencedAssemblies.Add ("System.Xaml.dll") ;
				parameters.ReferencedAssemblies.Add ("System.Xml.dll") ;
				parameters.ReferencedAssemblies.Add ("System.Xml.Linq.dll") ;

				//string dotNetSDKPath = AppDomain.CurrentDomain.BaseDirectory;
				string dotNetSDKPath =System.IO.Path.GetDirectoryName (Assembly.GetAssembly (typeof (MObject)).Location) + @"\" ;
				parameters.ReferencedAssemblies.Add (dotNetSDKPath + "openmayacpp.dll") ;
				parameters.ReferencedAssemblies.Add (dotNetSDKPath + "openmayacs.dll") ;

				CompilerResults results =compiler.CompileAssemblyFromSource (parameters, in_source) ;

				if ( results.Errors.Count > 0 ) {
					string sErrors ="Search Condition is invalid:\n" ;
					foreach ( CompilerError err in results.Errors )
						sErrors += err.ToString () + "\n" ;
					sErrors +="\nImportant! because we compile and run code on the fly,\n"
						+ "the current working directory *must* be the Maya directory where resides"
						+ "the Maya API .NET assemblies (I.e. openmayacpp.dll / openmayacs.dll).\n" ;
					MessageBox.Show (sErrors, "DAG Explorer", MessageBoxButton.OK, MessageBoxImage.Error) ;
				} else {
					object o =results.CompiledAssembly.CreateInstance ("Script") ;
					Type type =o.GetType () ;
					MethodInfo m =type.GetMethod ("Main") ;
					Object Result =m.Invoke (o, null) ;

					// Done with the temp assembly
					if ( File.Exists (tempPath) )
						File.Delete (tempPath) ;

					return (Result) ;
				}
			} catch ( Exception e ) {
				Console.WriteLine (e.ToString ()) ;
				// Done with the temp assembly
				if ( File.Exists (tempPath) )
					File.Delete (tempPath) ;
			}

			return (null) ;
		}

		#region Lambda expression & presets
		// Preset for all
		private void AllPreset_Click (object sender, RoutedEventArgs e) {
			textBox1.Text =@"// lambda expression that looks for all nodes.
true" ;
		}

		// Preset for finding meshes (by type)
		private void MeshPreset_Click (object sender, RoutedEventArgs e) {
			textBox1.Text =@"// lambda expression that looks for mesh node type.
dagpath.node.apiTypeStr == ""kMesh""" ;
		}

		// Preset for finding meshes with with a minimum polygon count
		private void PolyCntPreset_Click (object sender, RoutedEventArgs e) {
			textBox1.Text =@"// lambda statement that looks for meshes
// with a minimum polygon count.
{
  if ( dagpath.node.apiTypeStr == ""kMesh"" ) {
    var m =new MFnMesh (dagpath) ;
    return (m.numPolygons > 10) ;
  }
  return (false) ;
}" ;
		}

		// Preset for finding objects with a certain name
		private void NamePreset_Click (object sender, RoutedEventArgs e) {
			textBox1.Text =@"// lambda expression that selects 
// objects based on name prefix.
dagpath.partialPathName.StartsWith (""collision"")" ;
		}

		#endregion

	}

}
