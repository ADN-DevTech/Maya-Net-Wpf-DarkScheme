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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using System.Diagnostics;

using Autodesk.Maya;
using Autodesk.Maya.OpenMaya;
using Autodesk.Maya.OpenMayaUI;

namespace Autodesk.Maya.Samples.MayaWpfStandAlone {

	// Interaction logic for DAGExplorer.xaml
	public partial class DAGExplorer : Window {

		#region Traverse the Maya DAG
		public static Object SpecializeObject (MDagPath inObj) {
			if ( inObj != null ) {
				switch ( inObj.node.apiTypeStr ) {
					case "kMesh":
						return (new MFnMesh (inObj)) ;
					case "kNurbsSurface":
						return (new MFnNurbsSurface (inObj)) ;
					case "kNurbsCurve":
						return (new MFnNurbsCurve (inObj)) ;
					case "kTransform":
						return (new MFnTransform (inObj)) ;
					case "kCamera":
						return (new MFnCamera (inObj)) ;
					case "kSubdiv":
						return (new MFnSubd (inObj)) ;
				}
			}
			return (null) ;
		}

		private void GatherObjects (IEnumerable<MDagPath> inObjects, out LinkedList<MayaObject> ObjList, out HashSet<MayaObjPropId> Found) {
			ObjList =new LinkedList<MayaObject> () ;
			Found =new HashSet<MayaObjPropId> () ;

			foreach ( var Obj in inObjects ) {
				var mo =new MayaObject () ;
				// Init the object
				mo.type =Obj.node.apiTypeStr ;
				mo.name =Obj.partialPathName ;
				mo.properties =new Dictionary<string, MayaObjPropVal> () ;

				// The two first properties
				var mopi =new MayaObjPropId ("ObjName", null) ;
				Found.Add (mopi) ;
				var mopv =new MayaObjPropVal (null, Obj.partialPathName) ;
				mo.properties.Add ("ObjName", mopv) ;

				mopi =new MayaObjPropId ("ObjType", null) ;
				Found.Add (mopi) ;
				mopv =new MayaObjPropVal (null, Obj.node.apiTypeStr) ;
				mo.properties.Add ("ObjType", mopv) ;

				// The rest of the properties
				Object mobj =SpecializeObject (Obj) ;
				if ( mobj != null ) {
					var nodeProp =mobj.GetType ()
									.GetProperties ()
									.Where (pi => (pi.GetGetMethod () != null) && (pi.PropertyType == typeof (string) || pi.PropertyType == typeof (int) || pi.PropertyType == typeof (double) || pi.PropertyType == typeof (bool) || pi.PropertyType == typeof (float)))
									.Select (pi => new {
										Name = pi.Name,
										Value = pi.GetGetMethod ().Invoke (mobj, null),
										Type = pi.PropertyType
									}) ;

					foreach ( var pair in nodeProp ) {
						// Add the property to the global prop set found
						mopi =new MayaObjPropId (pair.Name, pair.Type) ;
						Found.Add (mopi) ;

						// Add the property value to the specific object
						mopv =new MayaObjPropVal (pair.Type, pair.Value.ToString ()) ;
						mo.properties.Add (pair.Name, mopv) ;
					}
				}

				var typeProp =Obj.GetType ()
									.GetProperties ()
									.Where (pi => (pi.GetGetMethod () != null) && (pi.PropertyType == typeof (string) || pi.PropertyType == typeof (int) || pi.PropertyType == typeof (double) || pi.PropertyType == typeof (bool) || pi.PropertyType == typeof (float)))
									.Select (pi => new {
										Name = pi.Name,
										Value = pi.GetGetMethod ().Invoke (Obj, null),
										Type = pi.PropertyType
									}) ;
				foreach ( var pair in typeProp ) {
					// Add the property to the global prop set found
					mopi =new MayaObjPropId (pair.Name, pair.Type) ;
					Found.Add (mopi) ;

					// Add the property value to the specific object
					mopv =new MayaObjPropVal (pair.Type, pair.Value.ToString ()) ;

					// Add only if the property hasn't been seen already
					if ( !mo.properties.ContainsKey (pair.Name) )
						mo.properties.Add (pair.Name, mopv) ;
				}
				ObjList.AddLast (mo) ;
			}
		}

		// Utility method: Get the first item of the selection list
		private static MDagPath GetFirstSelected () {
			var selected =MGlobal.activeSelectionList ;
			var it =new MItSelectionList (selected) ;
			if ( it.isDone )
				return (null) ;
			var path =new MDagPath () ;
			it.getDagPath (path) ;
			return (path) ;
		}

		#endregion

		#region Utilities to create a 3D model our of Maya's meshes
		public MeshGeometry3D MakeGeometry (MFnMesh fnMesh) {
			var r =new MeshGeometry3D () ;
			var mesh =new TriangleMeshAdapater (fnMesh) ;
			r.Positions =mesh.Points ;
			r.TriangleIndices =mesh.Indices ;
			r.Normals =mesh.Normals ;
			return (r) ;
		}

		public Material MakeMaterial (MFnMesh fnMesh) {
			MaterialGroup matGroup =new MaterialGroup () ;

			MObjectArray shaders =new MObjectArray() ;
			MIntArray indices =new MIntArray () ;
			fnMesh.getConnectedShaders (0, shaders, indices) ;
			for ( int i =0 ; i < shaders.length ; i++ ) {
				MFnDependencyNode shaderGroup =new MFnDependencyNode (shaders [i]) ;
				MPlug shaderPlug =shaderGroup.findPlug ("surfaceShader") ;
				MPlugArray connections =new MPlugArray () ;
				shaderPlug.connectedTo (connections, true, false) ;
				for ( int u =0 ; u < connections.length ; u++ ) {
					MFnDependencyNode depNode =new MFnDependencyNode (connections [u].node) ;

					//MPlug colorPlug =depNode.findPlug ("color") ;
					//MColor mcolor =new MColor () ;
					///*MPlugArray cc =new MPlugArray () ;
					//colorPlug.connectedTo (cc, true , false) ;
					//if ( cc.length > 0 ) {
					//    // Plug is driven by an input connection.
					//    for ( int v =0 ; v < cc.length ; v++ ) {
					//        MPlug color2Plug =cc [v] ;
					//        Console.WriteLine (color2Plug.numChildren) ;
					//        color2Plug.child (0).getValue (mcolor.r) ;
					//        color2Plug.child (1).getValue (mcolor.g) ;
					//        color2Plug.child (2).getValue (mcolor.b) ;
					//        //color2Plug.child (3).getValue (mcolor.a) ;
					//    }
					//} else {*/
					//    mcolor.r =colorPlug.child (0).asFloat () ;
					//    mcolor.g =colorPlug.child (1).asFloat () ;
					//    mcolor.b =colorPlug.child (2).asFloat () ;
					//    //colorPlug.child (3).getValue (mcolor.a) ;
					////}

					//MPlug trPlug =depNode.findPlug ("transparency") ;
					//float transparency =1.0f - trPlug.child (0).asFloat () ;
					////return new DiffuseMaterial (new SolidColorBrush (Color.FromScRgb (transparency, mcolor.r, mcolor.g, mcolor.b))) ;

					//DiffuseMaterial diffuse =new DiffuseMaterial (new SolidColorBrush (Color.FromScRgb (transparency, mcolor.r, mcolor.g, mcolor.b))) ;
					//colorPlug =depNode.findPlug ("ambientColor") ;
					//mcolor.r =colorPlug.child (0).asFloat () ;
					//mcolor.g =colorPlug.child (1).asFloat () ;
					//mcolor.b =colorPlug.child (2).asFloat () ;
					//diffuse.AmbientColor =Color.FromScRgb (transparency, mcolor.r, mcolor.g, mcolor.b) ;
					//matGroup.Children.Add (diffuse) ;

					//colorPlug =depNode.findPlug ("specularColor") ;
					//mcolor.r =colorPlug.child (0).asFloat () ;
					//mcolor.g =colorPlug.child (1).asFloat () ;
					//mcolor.b =colorPlug.child (2).asFloat () ;
					//MPlug powerPlug =depNode.findPlug ("cosinePower") ;

					//SpecularMaterial specular =new SpecularMaterial (new SolidColorBrush (Color.FromScRgb (1.0f, mcolor.r, mcolor.g, mcolor.b)), powerPlug.asDouble ()) ;
					//matGroup.Children.Add (specular) ;

					//EmissiveMaterial emissive =new EmissiveMaterial () ;
					//matGroup.Children.Add (emissive) ;

					try {
						MFnLambertShader lambert =new MFnLambertShader (connections [u].node) ;

						SolidColorBrush brush =new SolidColorBrush (Color.FromScRgb (1.0f - lambert.transparency.r, lambert.color.r, lambert.color.g, lambert.color.b)) ;
						brush.Opacity =1.0f - lambert.transparency.r ;
						DiffuseMaterial diffuse =new DiffuseMaterial (brush) ;
						diffuse.AmbientColor =Color.FromScRgb (1.0f - lambert.ambientColor.a, lambert.ambientColor.r, lambert.ambientColor.g, lambert.ambientColor.b) ;
						// no more attributes
						matGroup.Children.Add (diffuse) ;

						// No specular color

						EmissiveMaterial emissive =new EmissiveMaterial (new SolidColorBrush (Color.FromScRgb (1.0f - lambert.incandescence.a, lambert.incandescence.r, lambert.incandescence.g, lambert.incandescence.b))) ;
						// no more attributes
						matGroup.Children.Add (emissive) ;
					} catch {
					}

					//try {
					//    MFnReflectShader reflect =new MFnReflectShader (connections [u].node) ;

					//    SpecularMaterial specular =new SpecularMaterial (new SolidColorBrush (Color.FromScRgb (1.0f - reflect.specularColor.a, reflect.specularColor.r, reflect.specularColor.g, reflect.specularColor.b)), reflect.cosPower) ;
					//    // no more attributes
					//    matGroup.Children.Add (specular) ;
					//} catch {
					//}
					
					try {
						MFnPhongShader phong =new MFnPhongShader (connections [u].node) ;

						//See Lambert
						//SolidColorBrush brush =new SolidColorBrush (Color.FromScRgb (1.0f - phong.transparency.r, phong.color.r, phong.color.g, phong.color.b)) ;
						//brush.Opacity =1.0f - phong.transparency.r ;
						//DiffuseMaterial diffuse =new DiffuseMaterial (brush) ;
						//diffuse.AmbientColor =Color.FromScRgb (1.0f - phong.ambientColor.a, phong.ambientColor.r, phong.ambientColor.g, phong.ambientColor.b) ;
						//// no more attributes
						//matGroup.Children.Add (diffuse) ;

						SpecularMaterial specular =new SpecularMaterial (new SolidColorBrush (Color.FromScRgb (1.0f - phong.specularColor.a, phong.specularColor.r, phong.specularColor.g, phong.specularColor.b)), phong.cosPower) ;
						// no more attributes
						matGroup.Children.Add (specular) ;

						//See Lambert
						//EmissiveMaterial emissive =new EmissiveMaterial (new SolidColorBrush (Color.FromScRgb (1.0f - phong.incandescence.a, phong.incandescence.r, phong.incandescence.g, phong.incandescence.b))) ;
						//// no more attributes
						//matGroup.Children.Add (emissive) ;
					} catch {
					}

					// todo
					//try {
					//    MFnBlinnShader phong =new MFnBlinnShader (connections [u].node) ;

					//    //See Lambert
					//    //SolidColorBrush brush =new SolidColorBrush (Color.FromScRgb (1.0f - phong.transparency.r, phong.color.r, phong.color.g, phong.color.b)) ;
					//    //brush.Opacity =1.0f - phong.transparency.r ;
					//    //DiffuseMaterial diffuse =new DiffuseMaterial (brush) ;
					//    //diffuse.AmbientColor = Color.FromScRgb (1.0f - phong.ambientColor.a, phong.ambientColor.r, phong.ambientColor.g, phong.ambientColor.b) ;
					//    //// no more attributes
					//    //matGroup.Children.Add (diffuse) ;

					//    //See Lambert
					//    //EmissiveMaterial emissive =new EmissiveMaterial (new SolidColorBrush (Color.FromScRgb (1.0f - phong.incandescence.a, phong.incandescence.r, phong.incandescence.g, phong.incandescence.b))) ;
					//    //// no more attributes
					//    //matGroup.Children.Add (emissive) ;
					//} catch {
					//}
				}
			}

			// Default to Blue
			if ( matGroup.Children.Count == 0 ) 
				 matGroup.Children.Add (new DiffuseMaterial (new SolidColorBrush (Color.FromRgb (0, 0, 255)))) ;
			return (matGroup) ;
		}

		public GeometryModel3D MakeGeometryModel (Geometry3D geom, Material mat) {
			return (new GeometryModel3D (geom, mat)) ;
		}

		public Model3D MakeModel (MFnMesh mesh) {
			return (MakeGeometryModel (MakeGeometry (mesh), MakeMaterial (mesh))) ;
		}

		public ModelVisual3D MakeVisualModel (MDagPath path) {
			var mesh =new MFnMesh (path) ;
			var r =new ModelVisual3D () ;
			r.Content =MakeModel (mesh) ;
			r.Transform =new Transform3DGroup () ;
			Transform3DGroup transformGroup =r.Transform as Transform3DGroup ;

			MTransformationMatrix matrix =new MTransformationMatrix (path.inclusiveMatrix) ;
			//MVector tr =matrix.getTranslation (MSpace.Space.kWorld) ;
			//TranslateTransform3D translation =new TranslateTransform3D (tr.x, tr.y, tr.z) ;
			//transformGroup.Children.Add (translation) ;

			//double x =0, y =0, z =0, w =0 ;
			//matrix.getRotationQuaternion (ref x, ref y, ref z, ref w, MSpace.Space.kWorld) ;
			//QuaternionRotation3D rotation =new QuaternionRotation3D (new Quaternion (x, y, z, w)) ;
			//transformGroup.Children.Add (new RotateTransform3D (rotation)) ;

			//double [] scales =new double [3] ;
			//matrix.getScale (scales, MSpace.Space.kWorld) ;
			//ScaleTransform3D scale =new ScaleTransform3D (scales [0], scales [1], scales [2]) ;
			//transformGroup.Children.Add (scale) ;

			MMatrix mat =matrix.asMatrixProperty ;
			Matrix3D matrix3d =new Matrix3D (mat [0, 0], mat [0, 1], mat [0, 2], mat [0, 3],
											 mat [1, 0], mat [1, 1], mat [1, 2], mat [1, 3],
											 mat [2, 0], mat [2, 1], mat [2, 2], mat [2, 3],
											 mat [3, 0], mat [3, 1], mat [3, 2], mat [3, 3]) ;
			MatrixTransform3D matrixTransform = new MatrixTransform3D (matrix3d) ;
			transformGroup.Children.Add (matrixTransform) ;
			
			return (r) ;
		}

		#endregion

		#region Maya Camera and Lights
		public void ResetUpAxis () {
			upAxis =new Vector3D (MGlobal.upAxis.x, MGlobal.upAxis.y, MGlobal.upAxis.z) ;
		}

		public void ResetCamera () {
			//<PerspectiveCamera UpDirection="0,1,0" Position="1,1,1" LookDirection="-1,-1,-1" FieldOfView="45" />
			MDagPath cameraPath ;
			try {
				// Try with a Maya host first
				cameraPath =M3dView.active3dView.Camera ;
			} catch {
				// We are in standalone mode (WPF application)
				MSelectionList list =new MSelectionList () ;
				list.add ("persp") ;
				cameraPath =new MDagPath () ;
				list.getDagPath (0, cameraPath) ;
			}

			MFnCamera fnCamera =new MFnCamera (cameraPath) ;
			MPoint eyePoint =fnCamera.eyePoint (MSpace.Space.kWorld) ;
			MPoint centerOfInterestPoint =fnCamera.centerOfInterestPoint (MSpace.Space.kWorld) ;
			MVector direction =centerOfInterestPoint.minus (eyePoint) ;
			MVector upDirection =fnCamera.upDirection (MSpace.Space.kWorld) ;

			camera.Position =new Point3D (eyePoint.x, eyePoint.y, eyePoint.z) ;
			camera.LookDirection =new Vector3D (direction.x, direction.y, direction.z) ;
			MAngle fieldOfView =new MAngle (fnCamera.verticalFieldOfView) ; //verticalFieldOfView / horizontalFieldOfView
			camera.FieldOfView =fieldOfView.asDegrees ;
			camera.UpDirection =new Vector3D (upDirection.x, upDirection.y, upDirection.z) ;
			camera.NearPlaneDistance =fnCamera.nearClippingPlane ;
			camera.FarPlaneDistance =fnCamera.farClippingPlane ;
			camera.Transform =new Transform3DGroup () ;
			(camera.Transform as Transform3DGroup).Children.Add (new TranslateTransform3D (new Vector3D ())) ;
		}

		public void ResetLights () {
			//<AmbientLight Color="White" />
			//<DirectionalLight Color="White" Direction="-1,-1,-1" />
			//<PointLight Color="White" ConstantAttenuation="1" LinearAttenuation="1" Position="0,0,0" QuadraticAttenuation="1" Range="0" />
			//<SpotLight Color="White" ConstantAttenuation="1" Direction="-1,-1,-1" InnerConeAngle="10" LinearAttenuation="1" OuterConeAngle="10" Position="0,0,0" QuadraticAttenuation="1" Range="0" />
			lights.Children.Clear () ;

			MItDag dagIterator =new MItDag (MItDag.TraversalType.kDepthFirst, MFn.Type.kLight) ;
			for ( ; !dagIterator.isDone ; dagIterator.next () ) {
				MDagPath lightPath =new MDagPath () ;
				dagIterator.getPath (lightPath) ;

				MFnLight light =new MFnLight (lightPath) ;
				bool isAmbient =light.lightAmbient ;
				MColor mcolor =light.color ;
				Color color =Color.FromScRgb (1.0f, mcolor.r, mcolor.g, mcolor.b) ;
				if ( isAmbient ) {
					AmbientLight ambient =new AmbientLight (color) ;
					lights.Children.Add (ambient) ;
					continue ;
				}

				MFloatVector lightDirection =light.lightDirection (0, MSpace.Space.kWorld) ;
				Vector3D direction =new Vector3D (lightDirection.x, lightDirection.y, lightDirection.z) ;
				bool isDiffuse =light.lightDiffuse ;
				try {
					MFnDirectionalLight dirLight =new MFnDirectionalLight (lightPath) ;
					DirectionalLight directional =new DirectionalLight (color, direction) ;
					lights.Children.Add (directional) ;
					continue ;
				} catch {
				}

				MObject transformNode =lightPath.transform ;
				MFnDagNode transform =new MFnDagNode (transformNode) ;
				MTransformationMatrix matrix =new MTransformationMatrix (transform.transformationMatrix) ;
				double [] threeDoubles =new double [3] ;
				int rOrder =0 ; //MTransformationMatrix.RotationOrder rOrder ;
				matrix.getRotation (threeDoubles, out rOrder, MSpace.Space.kWorld) ;
				matrix.getScale (threeDoubles, MSpace.Space.kWorld) ;
				MVector pos =matrix.getTranslation (MSpace.Space.kWorld) ;
				Point3D position =new Point3D (pos.x, pos.y, pos.z) ;
				try {
					MFnPointLight pointLight =new MFnPointLight (lightPath) ;
					PointLight point =new PointLight (color, position) ;
					//point.ConstantAttenuation =pointLight. ; // LinearAttenuation / QuadraticAttenuation
					//point.Range =pointLight.rayDepthLimit ;
					lights.Children.Add (point) ;
					continue ;
				} catch {
				}

				try {
					MFnSpotLight spotLight =new MFnSpotLight (lightPath) ;
					MAngle InnerConeAngle =new MAngle (spotLight.coneAngle) ;
					MAngle OuterConeAngle =new MAngle (spotLight.penumbraAngle) ;
					SpotLight spot =new SpotLight (color, position, direction, OuterConeAngle.asDegrees, InnerConeAngle.asDegrees) ;
					spot.ConstantAttenuation =spotLight.dropOff ; // LinearAttenuation / QuadraticAttenuation
					//spot.Range =spotLight.rayDepthLimit ;
					lights.Children.Add (spot) ;
					continue ;
				} catch {
				}
			}
		}

		#endregion

	}

	#region Utility classes for the Object Property window
	public struct MayaObjPropId {
		public string name ;
		public Type type ;

		public MayaObjPropId (string inName, Type inType) {
			name =inName ;
			type =inType ;
		}
	}

	public struct MayaObjPropVal {
		public Type type ;
		public string value ;

		public MayaObjPropVal (Type inTP, string inVal) {
			type =inTP ;
			value =inVal ;
		}
	}

	public class MayaObject {
		public string name ;
		public string type ;
		public Dictionary<string, MayaObjPropVal> properties ;
	}

	#endregion


}
