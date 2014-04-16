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

// This line is not mandatory, but improves loading performances
[assembly: ExtensionPlugin (typeof (MayaWpfThemeTest.MyPlugin))]

namespace MayaWpfThemeTest {

	// This class is instantiated by Maya once and kept alive for the 
	// duration of the session. If you don't do any one time initialization 
	// then you should remove this class.
	// Its presence still improve load performance whilst you don't do any
	// initialization in it.
	public class MyPlugin : IExtensionPlugin {

		public bool InitializePlugin () {
			// Add one time initialization here
			// One common scenario is to setup a callback function here that 
			// unmanaged code can call. 
			// To do this:
			// 1. Export a function from unmanaged code that takes a function
			//    pointer and stores the passed in value in a global variable.
			// 2. Call this exported function in this function passing delegate.
			// 3. When unmanaged code needs the services of this managed module
			//    you simply call loadPlugin() and by the time loadPlugin 
			//    returns global function pointer is initialized to point to
			//    the C# delegate.
			// For more info see: 
			// http://msdn2.microsoft.com/en-US/library/5zwkzwf4(VS.80).aspx
			// http://msdn2.microsoft.com/en-us/library/44ey4b32(VS.80).aspx
			// http://msdn2.microsoft.com/en-US/library/7esfatk4.aspx
			// as well as some of the existing Maya managed apps.

			// Initialize your plug-in application here
			bool b =MayaTheme.Initialize (null) ;

			return (true) ;
		}

		public bool UninitializePlugin () {
			// Do plug-in application clean up here
			return (true) ;
		}

		public string GetMayaDotNetSdkBuildVersion () {
			// Function to return the Maya API version number
			// The actual plug-in can return an empty string by default
			return ("") ;
		}

	}

}
