﻿// (C) Copyright 2014 by Autodesk, Inc.
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

	// Utility Class for converting data containing a Maya MFnMesh into an object that is compatible
	// with the Windows Presentation framework. 
	public class TriangleMeshAdapater {
		public Int32Collection Indices ;
		public Point3DCollection Points ;
		public Vector3DCollection Normals ;

		public TriangleMeshAdapater (MFnMesh mesh) {
			MIntArray indices =new MIntArray () ;
			MIntArray triangleCounts =new MIntArray () ;
			MPointArray points =new MPointArray () ;

			mesh.getTriangles (triangleCounts, indices) ;
			mesh.getPoints (points) ;

			// Get the triangle indices
			Indices =new Int32Collection ((int)indices.length) ;
			for ( int i =0 ; i < indices.length ; ++i )
				Indices.Add (indices [i]) ;

			// Get the control points (vertices)
			Points =new Point3DCollection ((int)points.length);
			for ( int i =0 ; i < (int)points.length ; ++i ) {
				MPoint pt =points [i] ;
				Points.Add (new Point3D (pt.x, pt.y, pt.z)) ;
			}

			// Get the number of triangle faces and polygon faces 
			Debug.Assert (indices.length % 3 == 0) ;
			int triFaces =(int)indices.length / 3 ;
			int polyFaces =mesh.numPolygons;

			// We have normals per polygon, we want one per triangle. 
			Normals =new Vector3DCollection (triFaces) ;
			int nCurrentTriangle =0 ;

			// Iterate over each polygon
			for ( int i =0 ; i < polyFaces ; ++i ) {
				// Get the polygon normal
				var maya_normal =new MVector () ;
				mesh.getPolygonNormal ((int)i, maya_normal) ;
				var normal =new Vector3D (maya_normal.x, maya_normal.y, maya_normal.z) ;

				// Iterate over each tri in the current polygon
				int nTrisAtFace =triangleCounts [i] ;
				for ( int j =0 ; j < nTrisAtFace ; ++j ) {
					Debug.Assert (nCurrentTriangle < triFaces) ;
					Normals.Add (normal) ;
					nCurrentTriangle++ ;
				}
			}
			Debug.Assert (nCurrentTriangle == triFaces) ;
		}

	}

}
