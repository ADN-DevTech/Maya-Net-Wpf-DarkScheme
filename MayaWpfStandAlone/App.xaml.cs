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
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

using Autodesk.Maya.Runtime;
using Autodesk.Maya.OpenMaya;
using Autodesk.Maya;

// Important! because we compile and run code on the fly, the current working directory *must* be
// the Maya directory where resides the Maya API .NET assemblies (I.e. openmayacpp.dll / openmayacs.dll).

// and create a PostBuild event like this:
//		copy "$(TargetPath)" "C:\Program Files\Autodesk\Maya2014\bin"

namespace Autodesk.Maya.Samples.MayaWpfStandAlone {

	/// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

		void App_Startup(object sender, StartupEventArgs e) {
			try {
				MLibrary.initialize ("MayaWpfStandAlone");
				bool bSuccess =MayaTheme.Initialize (this);

				string fileName;
				string [] args = Environment.GetCommandLineArgs ();
				if ( args.Length <= 1 ) {
					Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog ();
					dlg.FileName = "Maya Files"; // Default file name
					dlg.DefaultExt = ".ma"; // Default file extension
					dlg.Filter = "Maya Files (.ma/.mb)|*.ma;*.mb|Maya ASCII (.ma)|*.ma|Maya Binary (.mb)|*.mb|All files (*.*)|*.*";
					Nullable<bool> result = dlg.ShowDialog ();
					if ( result == true )
						fileName = dlg.FileName;
					else
						return;
				} else {
					fileName = args [1];
				}

				MFileIO.newFile(true);
				fileName =fileName.Replace ('\\', '/');
				MFileIO.open(fileName);

			} catch (System.Exception ex) {
				MessageBox.Show (ex.Message, "Error during Maya API initialization. This program will exit");
				Application.Current.Shutdown ();
			}
		}

		void App_Exit(object sender, ExitEventArgs e) {
			try {
				MLibrary.cleanup();
			} catch ( System.Exception ex ) {
				MessageBox.Show (ex.Message, "Error during Maya API cleanup");
				return;
			}
		}

    }

}
