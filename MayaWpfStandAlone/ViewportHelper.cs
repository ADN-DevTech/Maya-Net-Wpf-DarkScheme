using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows.Controls;

namespace Autodesk.Maya.Samples.MayaWpfStandAlone {

	public static class ViewportHelper {

		// Returns a matrix for an asymmetric frustrum
		public static Matrix3D PerspectiveOffCenter (double left, double top, double right, double bottom, double near, double far) {
			Matrix3D persp =new Matrix3D () ;
			persp.M11 =2 * near / (right - left) ;
			persp.M22 =2 * near / (top - bottom) ;
			persp.M31 =(right + left) / (right - left) ;
			persp.M32 =(top + bottom) / (top - bottom) ;
			persp.M33 =far / (far - near) ;
			persp.M34 =-1.0 ;
			persp.M44 =0 ;
			persp.OffsetZ =near * far / (near - far) ;
			return (persp) ;
		}

		// Returns a symmetric view frustum
		public static Matrix3D Perspective (double width, double height, double near, double far) {
			Matrix3D persp =new Matrix3D () ;
			persp.M11 =2 * near / width ;
			persp.M22 =2 * near / height ;
			persp.M33 =far / (far - near) ;
			persp.M34 =-1.0 ;
			persp.M44 =0 ;
			persp.OffsetZ =near * far / (near - far) ;
			return (persp) ;
		}

		// Returns a symmetric view frustum
		public static Matrix3D PerspectiveFov (double fov, double aspectRatio, double near, double far) {
			Matrix3D persp =new Matrix3D () ;
			double yscale =1.0 / Math.Tan (fov * Math.PI / 180 / 2.0) ;
			double xscale =yscale / aspectRatio ;
			persp.M11 =xscale ;
			persp.M22 =yscale ;
			persp.M33 =far / (far - near) ;
			persp.M34 =-1.0 ;
			persp.M44 =0.0 ;
			persp.OffsetZ =near * far / (near - far) ;
			return (persp) ;
		}

		// Returns a matrix for an asymmetric frustrum
		public static Matrix3D OrthographicOffCenter (double left, double top, double right, double bottom, double near, double far) {
			Matrix3D ortho =new Matrix3D () ;
			ortho.M11 =2.0 / (right - left) ;
			ortho.M22 =2.0 / (top - bottom) ;
			ortho.M33 =1.0 / (near - far) ;
			ortho.OffsetX =(left + right) / (left - right) ;
			ortho.OffsetY =(bottom + top) / (bottom - top) ;
			ortho.OffsetZ =near / (near - far) ;
			return (ortho) ;
		}

		public static Matrix3D Orthographic (double width, double height, double near, double far) {
			Matrix3D ortho =new Matrix3D () ;
			ortho.M11 =2.0 / width ;
			ortho.M22 =2.0 / height ;
			ortho.M33 =1.0 / (near - far) ;
			ortho.OffsetZ =near / (near - far) ;
			return (ortho) ;
		}

		public static Matrix3D GetViewportTransform (this Viewport3D viewport) {
			return (new Matrix3D (
				viewport.ActualWidth / 2, 0, 0, 0,
				0, -viewport.ActualHeight / 2, 0, 0,
				0, 0, 1, 0,
				viewport.ActualWidth / 2, viewport.ActualHeight / 2, 0, 1
			)) ;
		}

		public static Matrix3D GetViewportTransform (this Viewport3DVisual viewport3DVisual) {
			return (new Matrix3D (
				viewport3DVisual.Viewport.Width / 2, 0, 0, 0,
				0, -viewport3DVisual.Viewport.Height / 2, 0, 0,
				0, 0, 1, 0,
				viewport3DVisual.Viewport.X + (viewport3DVisual.Viewport.Width / 2), viewport3DVisual.Viewport.Y + (viewport3DVisual.Viewport.Height / 2), 0, 1
			)) ;
		}

		public static Matrix3D GetTotalTransform (this Viewport3D viewport) {
			var transform =GetCameraTotalTransform (viewport) ;
			transform.Append (GetViewportTransform (viewport)) ;
			return (transform) ;
		}

		public static Matrix3D GetTotalTransform (this Viewport3DVisual viewport3DVisual) {
			var m =GetCameraTotalTransform (viewport3DVisual) ;
			m.Append (GetViewportTransform (viewport3DVisual)) ;
			return (m) ;
		}

		public static Matrix3D GetCameraTotalTransform (this Viewport3D viewport) {
			return (viewport.Camera.GetTotalTransform (viewport.ActualWidth / viewport.ActualHeight)) ;
		}

		public static Matrix3D GetCameraTotalTransform (this Viewport3DVisual viewport3DVisual) {
			return (viewport3DVisual.Camera.GetTotalTransform (viewport3DVisual.Viewport.Size.Width / viewport3DVisual.Viewport.Size.Height)) ;
		}

		public static Matrix3D GetViewMatrix (this Viewport3D viewport) {
			return (viewport.Camera.GetViewMatrix ()) ;
		}

		public static Matrix3D GetProjectionMatrix (this Viewport3D viewport) {
			return (viewport.Camera.GetProjectionMatrix (viewport.ActualHeight / viewport.ActualWidth)) ;
		}

	}

}
