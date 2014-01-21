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
using Autodesk.Maya.Runtime;
using Autodesk.Maya.OpenMaya;
using Autodesk.Maya.Samples;

// This line is mandatory to declare a new command in Maya
// You need to change the last parameter without your own
// node name and unique ID
[assembly: MPxCommandClass (typeof (Autodesk.Maya.Samples.MayaWpfPlugin.DAGExplorerCmd), "DAGExplorer")]

namespace Autodesk.Maya.Samples.MayaWpfPlugin {
	// This class is instantiated by Maya each time when a command 
	// is called by the user or a script.
	public class DAGExplorerCmd : MPxCommand, IMPxCommand {
		public DAGExplorer wnd;

		public override void doIt (MArgList argl) {
			wnd = new DAGExplorer ();
			//MayaTheme.SetMayaIcon (wnd);
			wnd.Show ();
		}

	}

}
