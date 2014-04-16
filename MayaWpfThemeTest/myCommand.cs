// (C) Copyright 2014 by Autodesk, Inc.
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted, 
// provided that the above copyright notice appears in all copies and 
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting 
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS. 
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC. 
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.

//- Written by Cyrille Fauvel, Autodesk Developer Network (ADN)
//- http://www.autodesk.com/joinadn
//- December 30th, 2013
//
using System;
using Autodesk.Maya;
using Autodesk.Maya.OpenMaya;

// This line is mandatory to declare a new command in Maya
// You need to change the last parameter without your own
// node name and unique ID
[assembly: MPxCommandClass (typeof (MayaWpfThemeTest.WpfThemeTestCmd), "WpfThemeTest")]

namespace MayaWpfThemeTest {

	// This class is instantiated by Maya each time when a command 
	// is called by the user or a script.
	public class WpfThemeTestCmd : MPxCommand, IMPxCommand {
		public WpfThemeTestWindow wnd ;

		public override void doIt (MArgList argl) {
			wnd =new WpfThemeTestWindow () ;
			//MayaTheme.SetMayaIcon (wnd);
			wnd.Show () ;
		}

	}

}
