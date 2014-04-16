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

// WPF: 3-D Graphics Overview
// http://msdn.microsoft.com/en-us/library/ms747437(v=vs.110).aspx
//
//<Viewport3D Name="viewport">
//	<Viewport3D.Camera>
//		<PerspectiveCamera x:Name="camera" FarPlaneDistance="" LookDirection="" UpDirection="" NearPlaneDistance="" Position="" FieldOfView="" />
//	</Viewport3D.Camera>
//	<Viewport3D.Children>
//		<ModelVisual3D x:Name="model">
//			<ModelVisual3D.Content>
//				<Model3DGroup x:Name="lights">
//					<Model3DGroup.Children>
//						<DirectionalLight Color="" Direction="" />
//					</Model3DGroup.Children>
//				</Model3DGroup>
//				<Model3DGroup x:Name="orbit">
//					<Model3DGroup.Children>
//						...
//					</Model3DGroup.Children>
//				</Model3DGroup>
//				<Model3DGroup x:Name="objects">
//					<Model3DGroup.Children>
//						<GeometryModel3D>
//							<GeometryModel3D.Geometry>
//								<MeshGeometry3D Positions="" Normals="" TextureCoordinates="" TriangleIndices="" />
//							</GeometryModel3D.Geometry>
//							<GeometryModel3D.Material>
//								<DiffuseMaterial><DiffuseMaterial.Brush><SolidColorBrush Color="" Opacity="" /></DiffuseMaterial.Brush></DiffuseMaterial>
//							</GeometryModel3D.Material>
//							<GeometryModel3D.Transform>
//								<TranslateTransform3D OffsetX="" OffsetY="" OffsetZ="" />
//							</GeometryModel3D.Transform>
//						</GeometryModel3D>
//					</Model3DGroup.Children>
//				</Model3DGroup >
//			</ModelVisual3D.Content>
//		</ModelVisual3D>
//	</Viewport3D.Children>
//</Viewport3D>

// 3D Rotation Methods for 3d viewport
// Bell’s Trackball, Shoemake’s Arcball and the Two-axis Valuator method
// http://www.cse.yorku.ca/~wolfgang/papers/comparerotation.pdf

namespace Autodesk.Maya.Samples.MayaWpfStandAlone {

	// Interaction logic for DAGExplorer.xaml
	public partial class DAGExplorer : Window {
		private const double zoomDeltaFactor =200.0d ;
		private Point _lastPos ;
		private bool _singleMeshPreviewed =true ;

		public Vector3D upAxis { get; set; }

		#region Controlling the 3D view camera
		private void Grid_MouseDown (object sender, MouseButtonEventArgs e) {
			Mouse.Capture (canvas, CaptureMode.Element) ;
			_lastPos =Mouse.GetPosition (viewport) ; //e.GetPosition ()
		}

		private void Grid_MouseUp (object sender, MouseButtonEventArgs e) {
			Mouse.Capture (canvas, CaptureMode.None) ;
		}

		private void Grid_MouseMove (object sender, MouseEventArgs e) {
			Point pos =Mouse.GetPosition (viewport) ;
			if ( e.LeftButton == MouseButtonState.Pressed )
				Viewport_Rotate (pos) ;
			else if ( e.MiddleButton == MouseButtonState.Pressed )
				Viewport_Pan (pos) ;
			_lastPos =pos ;
		}

		private void Viewport_Pan (Point actualPos) {
			Vector3D lastPos3D =ProjectToTrackball (_lastPos) ;
			Vector3D pos3D =ProjectToTrackball (actualPos) ;

			//Length(original_position - cam_position) / Length(offset_vector) = Length(zNearA - cam_position) / Length(zNearB - zNearA)
			//offset_vector = Length(original_position - cam_position) / Length(zNearA - cam_position) * (zNearB - zNearA)
			double halfFOV =(camera.FieldOfView / 2.0f) * (Math.PI / 180.0) ;
			double distanceToObject =((Vector3D)camera.Position).Length ; // Compute the world space distance from the camera to the object you want to pan
			double projectionToWorldScale =distanceToObject * Math.Tan (halfFOV) ;
			Vector mouseDeltaInScreenSpace =actualPos - _lastPos ; // The delta mouse in pixels that we want to pan
			Vector mouseDeltaInProjectionSpace =new Vector (mouseDeltaInScreenSpace.X * 2 / viewport.ActualWidth, mouseDeltaInScreenSpace.Y * 2 / viewport.ActualHeight) ; // ( the "*2" is because the projection space is from -1 to 1)
			Vector cameraDelta =-mouseDeltaInProjectionSpace * projectionToWorldScale ; // Go from normalized device coordinate space to world space (at origin)

			Vector3D tr =new Vector3D (0.0d, cameraDelta.Y, cameraDelta.X) ; // Remember we are up=<0,1,0>
			foreach ( Visual3D child in model.Children ) {
				Transform3DGroup transformGroup =child.Transform as Transform3DGroup ;
				transformGroup.Children.Add (new TranslateTransform3D (tr)) ; // Remember we are up=<0,1,0>
			}
		}

		private void Viewport_Rotate (Point actualPos) {
			Vector3D lastPos3D =ProjectToTrackball (_lastPos) ;
			Vector3D pos3D =ProjectToTrackball (actualPos) ;
			Vector3D axis =Vector3D.CrossProduct (lastPos3D, pos3D) ;
			double angle =Vector3D.AngleBetween (lastPos3D, pos3D) ;

			if ( axis.Length == 0 && angle == 0 )
				return ;
			Quaternion quat =new Quaternion (axis, angle) ;
			QuaternionRotation3D r =new QuaternionRotation3D (quat) ;
			foreach ( Visual3D child in model.Children ) {
				Transform3DGroup transformGroup =child.Transform as Transform3DGroup ;
				transformGroup.Children.Add (new RotateTransform3D (r)) ;
			}
		}

		// http://scv.bu.edu/documentation/presentations/visualizationworkshop08/materials/opengl/trackball.c
		// http://curis.ku.dk/ws/files/38552161/01260772.pdf
		private Vector3D ProjectToTrackball (Point pos) { // Project an <x, y> pair onto a sphere
			// Translate 0,0 to the center, so <x, y> is [<-1, -1> - <1, 1>]
			double x =pos.X / (viewport.ActualWidth / 2) - 1 ;
			double y =1 - pos.Y / (viewport.ActualHeight / 2) ; // Flip Y - up instead of down
			double q =Math.Pow (x, 2) + Math.Pow (y, 2) ;
			double z =0.0 ;
			// Math.Sqrt (q) <= ref / Math.Sqrt (2.0) ;
			// r =1
			if ( q <= 1 / 2.0 )
				z =Math.Sqrt (1 - q) ;
			else if ( q != 0 )
				z =1 / (2 * Math.Sqrt (q)) ;
			return (new Vector3D (x, y, z)) ; // with Z up
		}

		private void Grid_MouseWheel (object sender, MouseWheelEventArgs e) {
			Vector3D lookAt =camera.LookDirection ;
			//lookAt.Negate () ;
			lookAt.Normalize () ;
			lookAt *=e.Delta / zoomDeltaFactor ;
			Transform3DGroup transformGroup =camera.Transform as Transform3DGroup ;
			transformGroup.Children.Add (new TranslateTransform3D (lookAt)) ;
		}

		#endregion

		#region Viewport settings
		private void Home_Click (object sender, RoutedEventArgs e) {
			if ( e != null )
				e.Handled =true ;

			Transform3DGroup transformGroup =camera.Transform as Transform3DGroup ;
			transformGroup.Children.Clear () ;
		}

		private void Wireframe_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;
		}

		private void SmoothShade_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;
		}

		private void WireframeOnShaded_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;
		}

		private void Textured_Click (object sender, RoutedEventArgs e) {
			e.Handled =true ;
		}

		private void ambiantlightToggle_Checked (object sender, RoutedEventArgs e) {
			e.Handled =true ;
		}

		private void ambiantlightToggle_Unchecked (object sender, RoutedEventArgs e) {
			e.Handled =true ;
		}

		private void dirlightToggle_Checked (object sender, RoutedEventArgs e) {
			e.Handled =true ;
		}

		private void dirlightToggle_Unchecked (object sender, RoutedEventArgs e) {
			e.Handled =true ;
		}

		#endregion

	}


}
