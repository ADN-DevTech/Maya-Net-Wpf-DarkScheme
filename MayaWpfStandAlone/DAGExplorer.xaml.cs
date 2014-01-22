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
		private bool _mouseDown =false ;
		private Point _lastPos ;
		private bool _singleMeshPreviewed = true;

		public DAGExplorer () {
			InitializeComponent ();
		}

		private void Window_Loaded (object sender, RoutedEventArgs e) {
			AllPreset_Click (null, null) ;
		}

		private void Window_SizeChanged (object sender, SizeChangedEventArgs e) {
			//ColumnDefinition defcol =grid1.ColumnDefinitions [1];
			textBox1.Width = this.ActualWidth - grid1.ColumnDefinitions [0].ActualWidth - grid1.ColumnDefinitions [2].ActualWidth;
			//RowDefinition defrow =grid1.RowDefinitions [1];
			textBox1.Height = this.ActualHeight - grid1.RowDefinitions [0].ActualHeight - grid1.RowDefinitions [2].ActualHeight - grid1.RowDefinitions [3].ActualHeight;

			viewport.Width = this.ActualWidth - 20;
			viewport.Height = this.ActualHeight - 40;
		}

		// Utility method: This method runs a script written in a language for which we have the domain compiler
		public object Run (string in_lang, string in_source) {
			string tempPath = System.IO.Path.GetTempPath () + "DotNET-Script-Tmp";

			try {
				if ( !CodeDomProvider.IsDefinedLanguage (in_lang) ) {
					// No provider defined for this language
					string sMsg = "No compiler is defined for " + in_lang;
					Console.WriteLine (sMsg);
					return null;
				}

				CodeDomProvider compiler = CodeDomProvider.CreateProvider (in_lang);
				CompilerParameters parameters = new CompilerParameters ();
				parameters.GenerateExecutable = false;
				parameters.GenerateInMemory = true;
				parameters.OutputAssembly = tempPath;
				parameters.MainClass = "Script.Main";
				parameters.IncludeDebugInformation = false;

				parameters.ReferencedAssemblies.Add ("Microsoft.CSharp.dll");
				parameters.ReferencedAssemblies.Add ("System.dll");
				parameters.ReferencedAssemblies.Add ("System.Core.dll");
				parameters.ReferencedAssemblies.Add ("System.Data.dll");
				parameters.ReferencedAssemblies.Add ("System.Data.DataSetExtensions.dll");
				parameters.ReferencedAssemblies.Add ("System.Xaml.dll");
				parameters.ReferencedAssemblies.Add ("System.Xml.dll");
				parameters.ReferencedAssemblies.Add ("System.Xml.Linq.dll");

				//string dotNetSDKPath = AppDomain.CurrentDomain.BaseDirectory;
				string dotNetSDKPath = System.IO.Path.GetDirectoryName (Assembly.GetAssembly (typeof (MObject)).Location) + @"\";
				parameters.ReferencedAssemblies.Add (dotNetSDKPath + "openmayacpp.dll");
				parameters.ReferencedAssemblies.Add (dotNetSDKPath + "openmayacs.dll");

				CompilerResults results = compiler.CompileAssemblyFromSource (parameters, in_source);

				if ( results.Errors.Count > 0 ) {
					string sErrors = "Search Condition is invalid:\n";
					foreach ( CompilerError err in results.Errors ) {
						sErrors += err.ToString () + "\n";
					}
					sErrors += "\nImportant! because we compile and run code on the fly,\n"
						+ "the current working directory *must* be the Maya directory where resides"
						+ "the Maya API .NET assemblies (I.e. openmayacpp.dll / openmayacs.dll).\n";
					MessageBox.Show (sErrors, "DAG Explorer", MessageBoxButton.OK, MessageBoxImage.Error);
				} else {
					object o = results.CompiledAssembly.CreateInstance ("Script");
					Type type = o.GetType ();
					MethodInfo m = type.GetMethod ("Main");
					Object Result = m.Invoke (o, null);

					// Done with the temp assembly
					if ( File.Exists (tempPath) ) {
						File.Delete (tempPath);
					}

					return Result;
				}
			} catch ( Exception e ) {
				Console.WriteLine (e.ToString ());

				// Done with the temp assembly
				if ( File.Exists (tempPath) ) {
					File.Delete (tempPath);
				}
			}

			return null;
		}

		#region Traverse the Maya DAG
		public static Object SpecializeObject (MDagPath inObj) {
			if ( inObj != null ) {
				switch ( inObj.node.apiTypeStr ) {
					case "kMesh":
						return new MFnMesh (inObj);
					case "kNurbsSurface":
						return new MFnNurbsSurface (inObj);
					case "kNurbsCurve":
						return new MFnNurbsCurve (inObj);
					case "kTransform":
						return new MFnTransform (inObj);
					case "kCamera":
						return new MFnCamera (inObj);
					case "kSubdiv":
						return new MFnSubd (inObj);
				}
			}
			return null;
		}

		private void GatherObjects (IEnumerable<MDagPath> inObjects, out LinkedList<MayaObject> ObjList, out HashSet<MayaObjPropId> Found) {
			ObjList = new LinkedList<MayaObject> ();
			Found = new HashSet<MayaObjPropId> ();

			foreach ( var Obj in inObjects ) {
				var mo = new MayaObject ();
				// Init the object
				mo.type = Obj.node.apiTypeStr;
				mo.name = Obj.partialPathName;
				mo.properties = new Dictionary<string, MayaObjPropVal> ();

				// The two first properties
				var mopi = new MayaObjPropId ("ObjName", null);
				Found.Add (mopi);
				var mopv = new MayaObjPropVal (null, Obj.partialPathName);
				mo.properties.Add ("ObjName", mopv);

				mopi = new MayaObjPropId ("ObjType", null);
				Found.Add (mopi);
				mopv = new MayaObjPropVal (null, Obj.node.apiTypeStr);
				mo.properties.Add ("ObjType", mopv);

				// The rest of the properties
				Object mobj = SpecializeObject (Obj);
				if ( mobj != null ) {
					var nodeProp = mobj.GetType ()
										.GetProperties ()
										.Where (pi => (pi.GetGetMethod () != null) && (pi.PropertyType == typeof (string) || pi.PropertyType == typeof (int) || pi.PropertyType == typeof (double) || pi.PropertyType == typeof (bool) || pi.PropertyType == typeof (float)))
										.Select (pi => new {
											Name = pi.Name,
											Value = pi.GetGetMethod ().Invoke (mobj, null),
											Type = pi.PropertyType
										});

					foreach ( var pair in nodeProp ) {
						// Add the property to the global prop set found
						mopi = new MayaObjPropId (pair.Name, pair.Type);
						Found.Add (mopi);

						// Add the property value to the specific object
						mopv = new MayaObjPropVal (pair.Type, pair.Value.ToString ());
						mo.properties.Add (pair.Name, mopv);
					}
				}

				var typeProp = Obj.GetType ()
									.GetProperties ()
									.Where (pi => (pi.GetGetMethod () != null) && (pi.PropertyType == typeof (string) || pi.PropertyType == typeof (int) || pi.PropertyType == typeof (double) || pi.PropertyType == typeof (bool) || pi.PropertyType == typeof (float)))
									.Select (pi => new {
										Name = pi.Name,
										Value = pi.GetGetMethod ().Invoke (Obj, null),
										Type = pi.PropertyType
									});
				foreach ( var pair in typeProp ) {
					// Add the property to the global prop set found
					mopi = new MayaObjPropId (pair.Name, pair.Type);
					Found.Add (mopi);

					// Add the property value to the specific object
					mopv = new MayaObjPropVal (pair.Type, pair.Value.ToString ());

					// Add only if the property hasn't been seen already
					if ( !mo.properties.ContainsKey (pair.Name) )
						mo.properties.Add (pair.Name, mopv);
				}
				ObjList.AddLast (mo);
			}
		}

		// Utility method: Get the first item of the selection list
		private static MDagPath GetFirstSelected () {
			var selected = MGlobal.activeSelectionList;
			var it = new MItSelectionList (selected);
			if ( it.isDone )
				return (null) ;
			var path = new MDagPath ();
			it.getDagPath (path);
			return path;
		}
		#endregion

		// Search button is clicked: Traverse the DAG and put results into the Grid
		private void SearchButton_Click (object sender, RoutedEventArgs e) {
			// Script in which to embed the lambda written by the user
			string MyScript = @"using System;
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
                                }";
			// Run it
			Object ObjList = Run ("C#", MyScript);

			// If there was no error
			if ( ObjList != null ) {
				using ( new CursorSwitcher (null) ) {
					try {
						ResultGrid.Items.Clear ();
					} catch {
					}

					// Get whatever's returned by the script
					var ObjEnum = ObjList as IEnumerable<MDagPath>;
					// Were some objects returned?
					if ( !ObjEnum.Any<MDagPath> () )
						MessageBox.Show ("No object returned.", "DAG Explorer", MessageBoxButton.OK, MessageBoxImage.Information);
					else {
						LinkedList<MayaObject> myList;
						HashSet<MayaObjPropId> myProps;
						GatherObjects (ObjEnum, out myList, out myProps);
						int i;

						// Setup the grid data columns if it isn't done already
						if ( ResultGrid.Columns.Count < 2 ) {
							i = 0;
							DataGridTextColumn col;
							foreach ( var p in myProps ) {
								col = new DataGridTextColumn ();
								ResultGrid.Columns.Add (col);
								col.Header = p.name;
								col.Binding = new Binding ("[" + i + "]");

								i++;
							}
						}
						// Add all the rows, one per object
						foreach ( var Obj in myList ) {
							Object [] arr = new Object [myProps.Count];

							i = 0;
							foreach ( var p in myProps ) {
								MayaObjPropVal mopv;
								// Search for the property in the object
								if ( Obj.properties.TryGetValue (p.name, out mopv) ) {
									arr [i] = mopv.value;
								} else {
									arr [i] = "";
								}

								i++;
							}
							ResultGrid.Items.Add (arr);
						}
						TabControl1.BringIntoView ();
						tabItem1.IsSelected = true;
					}
				}
			}
		}

		#region Lambda expression & presets
		// Preset for all
		private void AllPreset_Click (object sender, RoutedEventArgs e) {
			textBox1.Text = @"// lambda expression that looks for all nodes.
true";
		}

		// Preset for finding meshes (by type)
		private void MeshPreset_Click (object sender, RoutedEventArgs e) {
			textBox1.Text = @"// lambda expression that looks for mesh node type.
dagpath.node.apiTypeStr == ""kMesh""";
		}

		// Preset for finding meshes with with a minimum polygon count
		private void PolyCntPreset_Click (object sender, RoutedEventArgs e) {
			textBox1.Text = @"// lambda statement that looks for meshes
// with a minimum polygon count.
{
  if (dagpath.node.apiTypeStr == ""kMesh"")
  {
    var m = new MFnMesh(dagpath);

    return (m.numPolygons > 10);
  }

  return false;
}";
		}

		// Preset for finding objects with a certain name
		private void NamePreset_Click (object sender, RoutedEventArgs e) {
			textBox1.Text = @"// lambda expression that selects 
// objects based on name prefix.
dagpath.partialPathName.StartsWith(""collision"")";
		}

		#endregion

		// When the focus changes in the result grid, select the node(s) in Maya
		private void ResultGrid_SelectionChanged (object sender, SelectionChangedEventArgs e) {
			e.Handled = true;
			foreach ( object item in e.RemovedItems ) {
				string ObjType = ((object [])item) [1].ToString ();
				string Name = ((object [])item) [0].ToString ();
				MGlobal.selectByName (Name, MGlobal.ListAdjustment.kRemoveFromList);
			}
			foreach ( object item in e.AddedItems ) {
				string ObjType = ((object [])item) [1].ToString ();
				string Name = ((object [])item) [0].ToString ();
				MGlobal.selectByName (Name, MGlobal.ListAdjustment.kAddToList);
			}
		}

		// Displays the object property window
		private void ResultGrid_MouseDoubleClick (object sender, MouseButtonEventArgs e) {
			var selected = GetFirstSelected ();
			if ( selected != null ) {
				HWNDWrapper mww;
				try {
					// Try with a Maya host first
					System.Windows.Forms.NativeWindow wnd = Runtime.MayaApplication.MainWindow;
					IntPtr mwh = MDockingStation.GetMayaMainWindow ();
					mww = new HWNDWrapper (mwh);
				} catch {
					// We are in standalone mode (WPF application)
					IntPtr mwh = new System.Windows.Interop.WindowInteropHelper (Application.Current.MainWindow).Handle;
					mww = new HWNDWrapper (mwh);
				}
				Form1 t = new Form1 (selected);
				t.ShowDialog (mww);
			}
		}

		// Switching to 3D preview
		private void TabControl_SelectionChanged (object sender, SelectionChangedEventArgs e) {
			e.Handled = true;
			if ( TabControl1.SelectedIndex == 1 ) { // If the result view was selected
				if ( !ResultGrid.HasItems )
					SearchButton_Click (null, null) ;
				return;
			}
			if ( TabControl1.SelectedIndex == 2 ) { // If the 3D view was selected
				ResetCamera ();
				ResetLights ();

				// Reset the model & transform(s)
				this.model.Children.Clear ();
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
					MItSelectionList it = new MItSelectionList (MGlobal.activeSelectionList);
					for ( ; !it.isDone ; it.next () ) {
						MDagPath path = new MDagPath ();
						it.getDagPath (path);
						if ( path.node.apiTypeStr == "kMesh" )
							model.Children.Add (MakeVisualModel (path));
					}
				}
				return;
			}
		}

		#region Utilities to create a 3D model our of Maya's meshes
		public MeshGeometry3D MakeGeometry (MFnMesh fnMesh) {
			var r = new MeshGeometry3D ();
			var mesh = new TriangleMeshAdapater (fnMesh);
			r.Positions = mesh.Points;
			r.TriangleIndices = mesh.Indices;
			r.Normals = mesh.Normals;
			return r;
		}

		public Material MakeMaterial (MFnMesh fnMesh) {
			MaterialGroup matGroup = new MaterialGroup ();

			MObjectArray shaders =new MObjectArray() ;
			MIntArray indices = new MIntArray ();
			fnMesh.getConnectedShaders(0, shaders, indices);
			for ( int i =0 ; i < shaders.length ; i++ ) {
				MFnDependencyNode shaderGroup = new MFnDependencyNode (shaders [i]) ;
				MPlug shaderPlug = shaderGroup.findPlug ("surfaceShader");
				MPlugArray connections = new MPlugArray (); 
				shaderPlug.connectedTo (connections, true, false);
				for ( int u =0 ; u < connections.length ; u++ ) {
					MFnDependencyNode depNode =new MFnDependencyNode (connections[u].node) ;

					//MPlug colorPlug = depNode.findPlug ("color");
					//MColor mcolor =new MColor ();
					///*MPlugArray cc =new MPlugArray ();
					//colorPlug.connectedTo (cc, true , false);
					//if ( cc.length > 0 ) {
					//    // Plug is driven by an input connection.
					//    for ( int v = 0 ; v < cc.length ; v++ ) {
					//        MPlug color2Plug = cc [v];
					//        Console.WriteLine (color2Plug.numChildren);
					//        color2Plug.child (0).getValue (mcolor.r);
					//        color2Plug.child (1).getValue (mcolor.g);
					//        color2Plug.child (2).getValue (mcolor.b);
					//        //color2Plug.child (3).getValue (mcolor.a);
					//    }
					//} else {*/
					//    mcolor.r =colorPlug.child (0).asFloat ();
					//    mcolor.g =colorPlug.child (1).asFloat ();
					//    mcolor.b = colorPlug.child (2).asFloat ();
					//    //colorPlug.child (3).getValue (mcolor.a);
					////}

					//MPlug trPlug = depNode.findPlug ("transparency");
					//float transparency = 1.0f - trPlug.child (0).asFloat ();
					////return new DiffuseMaterial (new SolidColorBrush (Color.FromScRgb (transparency, mcolor.r, mcolor.g, mcolor.b)));

					//DiffuseMaterial diffuse =new DiffuseMaterial (new SolidColorBrush (Color.FromScRgb (transparency, mcolor.r, mcolor.g, mcolor.b)));
					//colorPlug = depNode.findPlug ("ambientColor");
					//mcolor.r = colorPlug.child (0).asFloat ();
					//mcolor.g = colorPlug.child (1).asFloat ();
					//mcolor.b = colorPlug.child (2).asFloat ();
					//diffuse.AmbientColor = Color.FromScRgb (transparency, mcolor.r, mcolor.g, mcolor.b);
					//matGroup.Children.Add (diffuse);

					//colorPlug = depNode.findPlug ("specularColor");
					//mcolor.r =colorPlug.child (0).asFloat ();
					//mcolor.g =colorPlug.child (1).asFloat ();
					//mcolor.b = colorPlug.child (2).asFloat ();
					//MPlug powerPlug = depNode.findPlug ("cosinePower");

					//SpecularMaterial specular = new SpecularMaterial (new SolidColorBrush (Color.FromScRgb (1.0f, mcolor.r, mcolor.g, mcolor.b)), powerPlug.asDouble ());
					//matGroup.Children.Add (specular);

					//EmissiveMaterial emissive = new EmissiveMaterial ();
					//matGroup.Children.Add (emissive);

					try {
						MFnLambertShader lambert = new MFnLambertShader (connections [u].node);

						SolidColorBrush brush = new SolidColorBrush (Color.FromScRgb (1.0f - lambert.transparency.r, lambert.color.r, lambert.color.g, lambert.color.b));
						brush.Opacity = 1.0f - lambert.transparency.r;
						DiffuseMaterial diffuse = new DiffuseMaterial (brush);
						diffuse.AmbientColor = Color.FromScRgb (1.0f - lambert.ambientColor.a, lambert.ambientColor.r, lambert.ambientColor.g, lambert.ambientColor.b);
						// no more attributes
						matGroup.Children.Add (diffuse);

						// No specular color

						EmissiveMaterial emissive = new EmissiveMaterial (new SolidColorBrush (Color.FromScRgb (1.0f - lambert.incandescence.a, lambert.incandescence.r, lambert.incandescence.g, lambert.incandescence.b)));
						// no more attributes
						matGroup.Children.Add (emissive);
					} catch {
					}

					//try {
					//    MFnReflectShader reflect = new MFnReflectShader (connections [u].node);

					//    SpecularMaterial specular = new SpecularMaterial (new SolidColorBrush (Color.FromScRgb (1.0f - reflect.specularColor.a, reflect.specularColor.r, reflect.specularColor.g, reflect.specularColor.b)), reflect.cosPower);
					//    // no more attributes
					//    matGroup.Children.Add (specular);
					//} catch {
					//}
					
					try {
						MFnPhongShader phong = new MFnPhongShader (connections [u].node);

						//See Lambert
						//SolidColorBrush brush = new SolidColorBrush (Color.FromScRgb (1.0f - phong.transparency.r, phong.color.r, phong.color.g, phong.color.b));
						//brush.Opacity = 1.0f - phong.transparency.r;
						//DiffuseMaterial diffuse = new DiffuseMaterial (brush);
						//diffuse.AmbientColor = Color.FromScRgb (1.0f - phong.ambientColor.a, phong.ambientColor.r, phong.ambientColor.g, phong.ambientColor.b);
						//// no more attributes
						//matGroup.Children.Add (diffuse);

						SpecularMaterial specular = new SpecularMaterial (new SolidColorBrush (Color.FromScRgb (1.0f - phong.specularColor.a, phong.specularColor.r, phong.specularColor.g, phong.specularColor.b)), phong.cosPower);
						// no more attributes
						matGroup.Children.Add (specular);

						//See Lambert
						//EmissiveMaterial emissive = new EmissiveMaterial (new SolidColorBrush (Color.FromScRgb (1.0f - phong.incandescence.a, phong.incandescence.r, phong.incandescence.g, phong.incandescence.b)));
						//// no more attributes
						//matGroup.Children.Add (emissive);
					} catch {
					}

					// todo
					//try {
					//    MFnBlinnShader phong = new MFnBlinnShader (connections [u].node);

					//    //See Lambert
					//    //SolidColorBrush brush = new SolidColorBrush (Color.FromScRgb (1.0f - phong.transparency.r, phong.color.r, phong.color.g, phong.color.b));
					//    //brush.Opacity = 1.0f - phong.transparency.r;
					//    //DiffuseMaterial diffuse = new DiffuseMaterial (brush);
					//    //diffuse.AmbientColor = Color.FromScRgb (1.0f - phong.ambientColor.a, phong.ambientColor.r, phong.ambientColor.g, phong.ambientColor.b);
					//    //// no more attributes
					//    //matGroup.Children.Add (diffuse);

					//    //See Lambert
					//    //EmissiveMaterial emissive = new EmissiveMaterial (new SolidColorBrush (Color.FromScRgb (1.0f - phong.incandescence.a, phong.incandescence.r, phong.incandescence.g, phong.incandescence.b)));
					//    //// no more attributes
					//    //matGroup.Children.Add (emissive);
					//} catch {
					//}
				}
			}

			// Default to Blue
			if ( matGroup.Children.Count != 0 ) 
				 matGroup.Children.Add (new DiffuseMaterial (new SolidColorBrush (Color.FromRgb (0, 0, 255))));
			return (matGroup);
		}

		public GeometryModel3D MakeGeometryModel (Geometry3D geom, Material mat) {
			return new GeometryModel3D (geom, mat);
		}

		public Model3D MakeModel (MFnMesh mesh) {
			return MakeGeometryModel (MakeGeometry (mesh), MakeMaterial (mesh));
		}

		public ModelVisual3D MakeVisualModel (MDagPath path) {
			var mesh = new MFnMesh (path);
			var r = new ModelVisual3D ();
			r.Content = MakeModel (mesh);
			r.Transform = new Transform3DGroup ();
			Transform3DGroup transformGroup = r.Transform as Transform3DGroup;

			MTransformationMatrix matrix = new MTransformationMatrix (path.inclusiveMatrix);
			//MVector tr = matrix.getTranslation (MSpace.Space.kWorld);
			//TranslateTransform3D translation = new TranslateTransform3D (tr.x, tr.y, tr.z);
			//transformGroup.Children.Add (translation);

			//double x =0, y =0, z =0, w =0 ;
			//matrix.getRotationQuaternion (ref x, ref y, ref z, ref w, MSpace.Space.kWorld);
			//QuaternionRotation3D rotation = new QuaternionRotation3D (new Quaternion (x, y, z, w));
			//transformGroup.Children.Add (new RotateTransform3D (rotation));

			//double [] scales =new double [3] ;
			//matrix.getScale (scales, MSpace.Space.kWorld);
			//ScaleTransform3D scale = new ScaleTransform3D (scales [0], scales [1], scales [2]);
			//transformGroup.Children.Add (scale);

			MMatrix mat =matrix.asMatrixProperty ;
			Matrix3D matrix3d = new Matrix3D (mat [0, 0], mat [0, 1], mat [0, 2], mat [0, 3],
											 mat [1, 0], mat [1, 1], mat [1, 2], mat [1, 3],
											 mat [2, 0], mat [2, 1], mat [2, 2], mat [2, 3],
											 mat [3, 0], mat [3, 1], mat [3, 2], mat [3, 3]);
			MatrixTransform3D matrixTransform = new MatrixTransform3D (matrix3d);
			transformGroup.Children.Add (matrixTransform);
			
			return r;
		}

		#endregion

		#region Maya Camera and Lights
		public void ResetCamera () {
			//<PerspectiveCamera UpDirection="0,1,0" Position="1,1,1" LookDirection="-1,-1,-1" FieldOfView="45" />

			MDagPath cameraPath;
			try {
				// Try with a Maya host first
				cameraPath = M3dView.active3dView.Camera;
			} catch {
				// We are in standalone mode (WPF application)
				MSelectionList list = new MSelectionList ();
				list.add ("persp");
				cameraPath = new MDagPath ();
				list.getDagPath (0, cameraPath);
			}

			MFnCamera fnCamera = new MFnCamera (cameraPath);
			MPoint eyePoint = fnCamera.eyePoint (MSpace.Space.kWorld);
			MPoint centerOfInterestPoint = fnCamera.centerOfInterestPoint (MSpace.Space.kWorld);
			MVector direction = centerOfInterestPoint.minus (eyePoint);
			MVector upDirection = fnCamera.upDirection (MSpace.Space.kWorld);

			camera.Position = new Point3D (eyePoint.x, eyePoint.y, eyePoint.z);
			camera.LookDirection = new Vector3D (direction.x, direction.y, direction.z);
			MAngle fieldOfView = new MAngle (fnCamera.verticalFieldOfView); //verticalFieldOfView / horizontalFieldOfView
			camera.FieldOfView = fieldOfView.asDegrees;
			camera.UpDirection = new Vector3D (upDirection.x, upDirection.y, upDirection.z);
			camera.NearPlaneDistance = fnCamera.nearClippingPlane;
			camera.FarPlaneDistance = fnCamera.farClippingPlane;
			camera.Transform = new Transform3DGroup ();
		}

		public void ResetLights () {
			//<AmbientLight Color="White" />
			//<DirectionalLight Color="White" Direction="-1,-1,-1" />
			//<PointLight Color="White" ConstantAttenuation="1" LinearAttenuation="1" Position="0,0,0" QuadraticAttenuation="1" Range="0" />
			//<SpotLight Color="White" ConstantAttenuation="1" Direction="-1,-1,-1" InnerConeAngle="10" LinearAttenuation="1" OuterConeAngle="10" Position="0,0,0" QuadraticAttenuation="1" Range="0" />
			lights.Children.Clear ();

			MItDag dagIterator = new MItDag (MItDag.TraversalType.kDepthFirst, MFn.Type.kLight);
			for ( ; !dagIterator.isDone ; dagIterator.next () ) {
				MDagPath lightPath = new MDagPath ();
				dagIterator.getPath (lightPath);

				MFnLight light = new MFnLight (lightPath);
				bool isAmbient = light.lightAmbient;
				MColor mcolor = light.color;
				Color color = Color.FromScRgb (1.0f, mcolor.r, mcolor.g, mcolor.b);
				if ( isAmbient ) {
					AmbientLight ambient = new AmbientLight (color);
					lights.Children.Add (ambient);
					continue;
				}

				MFloatVector lightDirection = light.lightDirection (0, MSpace.Space.kWorld);
				Vector3D direction = new Vector3D (lightDirection.x, lightDirection.y, lightDirection.z);
				bool isDiffuse = light.lightDiffuse;
				try {
					MFnDirectionalLight dirLight = new MFnDirectionalLight (lightPath);
					DirectionalLight directional = new DirectionalLight (color, direction);
					lights.Children.Add (directional);
					continue;
				} catch {
				}

				MObject transformNode = lightPath.transform;
				MFnDagNode transform = new MFnDagNode (transformNode);
				MTransformationMatrix matrix = new MTransformationMatrix (transform.transformationMatrix);
				double [] threeDoubles = new double [3];
				int rOrder = 0; //MTransformationMatrix.RotationOrder rOrder ;
				matrix.getRotation (threeDoubles, out rOrder, MSpace.Space.kWorld);
				matrix.getScale (threeDoubles, MSpace.Space.kWorld);
				MVector pos = matrix.getTranslation (MSpace.Space.kWorld);
				Point3D position = new Point3D (pos.x, pos.y, pos.z);
				try {
					MFnPointLight pointLight = new MFnPointLight (lightPath);
					PointLight point = new PointLight (color, position);
					//point.ConstantAttenuation = pointLight.; // LinearAttenuation / QuadraticAttenuation
					//point.Range = pointLight.rayDepthLimit;
					lights.Children.Add (point);
					continue;
				} catch {
				}

				try {
					MFnSpotLight spotLight = new MFnSpotLight (lightPath);
					MAngle InnerConeAngle = new MAngle (spotLight.coneAngle);
					MAngle OuterConeAngle = new MAngle (spotLight.penumbraAngle);
					SpotLight spot = new SpotLight (color, position, direction, OuterConeAngle.asDegrees, InnerConeAngle.asDegrees);
					spot.ConstantAttenuation = spotLight.dropOff; // LinearAttenuation / QuadraticAttenuation
					//spot.Range =spotLight.rayDepthLimit ;
					lights.Children.Add (spot);
					continue;
				} catch {
				}
			}
		}

		#endregion

		#region Controlling the 3D view camera
		private void Grid_MouseDown (object sender, MouseButtonEventArgs e) {
			if ( e.LeftButton != MouseButtonState.Pressed )
				return;
			_mouseDown = true;
			Point pos = Mouse.GetPosition (viewport);
			_lastPos = new Point (pos.X - viewport.ActualWidth / 2, viewport.ActualHeight / 2 - pos.Y);
		}

		private void Grid_MouseUp (object sender, MouseButtonEventArgs e) {
			_mouseDown = false;
		}

		private void Grid_MouseMove (object sender, MouseEventArgs e) {
			if ( !_mouseDown )
				return ;
			Point pos = Mouse.GetPosition (viewport);
			Point actualPos = new Point (pos.X - viewport.ActualWidth / 2, viewport.ActualHeight / 2 - pos.Y);
			double dx = actualPos.X - _lastPos.X, dy = actualPos.Y - _lastPos.Y;
			double mouseAngle = 0;
			if ( dx != 0 && dy != 0 ) {
				mouseAngle = Math.Asin (Math.Abs (dy) / Math.Sqrt (Math.Pow (dx, 2) + Math.Pow (dy, 2)));
				if ( dx < 0 && dy > 0 )
					mouseAngle += Math.PI / 2;
				else if ( dx < 0 && dy < 0 )
					mouseAngle += Math.PI;
				else if ( dx > 0 && dy < 0 )
					mouseAngle += Math.PI * 1.5;
			} else if ( dx == 0 && dy != 0 ) {
				mouseAngle = Math.Sign (dy) > 0 ? Math.PI / 2 : Math.PI * 1.5;
			} else if ( dx != 0 && dy == 0 ) {
				mouseAngle = Math.Sign (dx) > 0 ? 0 : Math.PI;
			}
			double axisAngle = mouseAngle + Math.PI / 2;
			Vector3D axis = new Vector3D (Math.Cos (axisAngle) * 4, Math.Sin (axisAngle) * 4, 0);
			double rotation = 0.01 * Math.Sqrt (Math.Pow (dx, 2) + Math.Pow (dy, 2));

			QuaternionRotation3D r = new QuaternionRotation3D (new Quaternion (axis, rotation * 180 / Math.PI));
			foreach ( Visual3D child in model.Children ) {
				Transform3DGroup transformGroup = child.Transform as Transform3DGroup;
				transformGroup.Children.Add (new RotateTransform3D (r));
			}
			_lastPos = actualPos;
		}

		private void Grid_MouseWheel (object sender, MouseWheelEventArgs e) {
			camera.Position = new Point3D (camera.Position.X - e.Delta / 250.0d, camera.Position.Y - e.Delta / 250.0d, camera.Position.Z - e.Delta / 250.0d);
		}

		#endregion

	}

	#region Utility classes for the Object Property window
	public struct MayaObjPropId {
		public string name;
		public Type type;

		public MayaObjPropId (string inName, Type inType) {
			name = inName;
			type = inType;
		}
	};

	public struct MayaObjPropVal {
		public Type type;
		public string value;

		public MayaObjPropVal (Type inTP, string inVal) {
			type = inTP;
			value = inVal;
		}
	}

	public class MayaObject {
		public string name;
		public string type;
		public Dictionary<string, MayaObjPropVal> properties;
	}

	// Utility Class for converting data containing a Maya MFnMesh into an object that is compatible
	// with the Windows Presentation framework. 
	public class TriangleMeshAdapater {
		public Int32Collection Indices;
		public Point3DCollection Points;
		public Vector3DCollection Normals;

		public TriangleMeshAdapater (MFnMesh mesh) {
			MIntArray indices = new MIntArray ();
			MIntArray triangleCounts = new MIntArray ();
			MPointArray points = new MPointArray ();

			mesh.getTriangles (triangleCounts, indices);
			mesh.getPoints (points);

			// Get the triangle indices
			Indices = new Int32Collection ((int)indices.length);
			for ( int i = 0 ; i < indices.length ; ++i )
				Indices.Add (indices [i]);

			// Get the control points (vertices)
			Points = new Point3DCollection ((int)points.length);
			for ( int i = 0 ; i < (int)points.length ; ++i ) {
				MPoint pt = points [i];
				Points.Add (new Point3D (pt.x, pt.y, pt.z));
			}

			// Get the number of triangle faces and polygon faces 
			Debug.Assert (indices.length % 3 == 0);
			int triFaces = (int)indices.length / 3;
			int polyFaces = mesh.numPolygons;

			// We have normals per polygon, we want one per triangle. 
			Normals = new Vector3DCollection (triFaces);
			int nCurrentTriangle = 0;

			// Iterate over each polygon
			for ( int i = 0 ; i < polyFaces ; ++i ) {
				// Get the polygon normal
				var maya_normal = new MVector ();
				mesh.getPolygonNormal ((int)i, maya_normal);
				var normal = new Vector3D (maya_normal.x, maya_normal.y, maya_normal.z);

				// Iterate over each tri in the current polygon
				int nTrisAtFace = triangleCounts [i];
				for ( int j = 0 ; j < nTrisAtFace ; ++j ) {
					Debug.Assert (nCurrentTriangle < triFaces);
					Normals.Add (normal);
					nCurrentTriangle++;
				}
			}
			Debug.Assert (nCurrentTriangle == triFaces);
		}
	}

	#endregion

	#region Utilities
	// Wrapper for the IntPtr/IWin32Window
	public class HWNDWrapper : System.Windows.Forms.IWin32Window {
		private IntPtr hwnd;

		public HWNDWrapper (IntPtr h) {
			hwnd = h;
		}

		public IntPtr Handle {
			get {
				return hwnd;
			}
		}
	}

	// Cursor switcher utility
	public class CursorSwitcher : IDisposable {
		private Cursor _previousCursor;

		public CursorSwitcher (Cursor cursor) {
			_previousCursor = Mouse.OverrideCursor;
			if ( cursor == null )
				Mouse.OverrideCursor = Cursors.Wait;
			else
				Mouse.OverrideCursor = cursor;
		}

		public void Dispose () {
			Mouse.OverrideCursor = _previousCursor;
		}

	}

	#endregion

}
