using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;
using SolidWorksTools;
using System;
using System.Collections;
using System.Runtime.InteropServices;


namespace SWSNG
{
	[Guid("F28A90F1-3E21-48A8-8A79-5BC7C11B0441"), ComVisible(true)]
	[SwAddin(
			Description = "SolidWorks CAD add-in for the serial number generator SWSNG",
			Title = "SWSNG: SW-CAD add-in",
			LoadAtStartup = true
			)]
	public class SwAddin : ISwAddin
	{
		#region <Local Variables>
		ISldWorks iSwApp = null;
		int addinID = 0;
		#endregion </Local Variables>

		#region <Event Handler Variables>
		Hashtable openDocs = new Hashtable();
		SldWorks SwEventPtr = null;
		#endregion </Event Handler Variables>

		#region <Public Properties>
		public ISldWorks SwApp
		{
			get { return iSwApp; }
		}

		public Hashtable OpenDocs
		{
			get { return openDocs; }
		}
		#endregion </Public Properties>

		#region <SolidWorks Registration>
		#region <Register the DLL>
		[ComRegisterFunctionAttribute]
		public static void RegisterFunction(Type t)
		{
			#region <Get Custom Attribute: SwAddinAttribute>
			SwAddinAttribute SWattr = null;
			Type type = typeof(SwAddin);

			foreach (System.Attribute attr in type.GetCustomAttributes(false))
			{
				if (attr is SwAddinAttribute)
				{
					SWattr = attr as SwAddinAttribute;
					break;
				}
			}
			#endregion </Get Custom Attribute: SwAddinAttribute>

			#region <Register the DLL>
			try
			{
				Microsoft.Win32.RegistryKey hklm = Microsoft.Win32.Registry.LocalMachine;
				Microsoft.Win32.RegistryKey hkcu = Microsoft.Win32.Registry.CurrentUser;

				string keyname = "SOFTWARE\\SolidWorks\\Addins\\{" + t.GUID.ToString() + "}";
				Microsoft.Win32.RegistryKey addinkey = hklm.CreateSubKey(keyname);
				addinkey.SetValue(null, 0);

				addinkey.SetValue("Description", SWattr.Description);
				addinkey.SetValue("Title", SWattr.Title);

				keyname = "Software\\SolidWorks\\AddInsStartup\\{" + t.GUID.ToString() + "}";
				addinkey = hkcu.CreateSubKey(keyname);
				addinkey.SetValue(null, Convert.ToInt32(SWattr.LoadAtStartup), Microsoft.Win32.RegistryValueKind.DWord);
			}
			catch (System.NullReferenceException nl)
			{
				Console.WriteLine("There was a problem registering this dll: SWattr is null. \n\"" + nl.Message + "\"");
				System.Windows.Forms.MessageBox.Show("There was a problem registering this dll: SWattr is null.\n\"" + nl.Message + "\"");
			}

			catch (System.Exception ex)
			{
				Console.WriteLine(ex.Message);
				System.Windows.Forms.MessageBox.Show("There was a problem registering the function: \n\"" + ex.Message + "\"");
			}
			#endregion </Register the DLL>
		}
		#endregion </Register the DLL>

		#region <Unregister the DLL>
		[ComUnregisterFunctionAttribute]
		public static void UnregisterFunction(Type t)
		{
			try
			{
				Microsoft.Win32.RegistryKey hklm = Microsoft.Win32.Registry.LocalMachine;
				Microsoft.Win32.RegistryKey hkcu = Microsoft.Win32.Registry.CurrentUser;

				string keyname = "SOFTWARE\\SolidWorks\\Addins\\{" + t.GUID.ToString() + "}";
				hklm.DeleteSubKey(keyname);

				keyname = "Software\\SolidWorks\\AddInsStartup\\{" + t.GUID.ToString() + "}";
				hkcu.DeleteSubKey(keyname);
			}
			catch (System.NullReferenceException nl)
			{
				Console.WriteLine("There was a problem unregistering this dll: " + nl.Message);
				System.Windows.Forms.MessageBox.Show("There was a problem unregistering this dll: \n\"" + nl.Message + "\"");
			}
			catch (System.Exception ex)
			{
				Console.WriteLine("There was a problem unregistering this dll: " + ex.Message);
				System.Windows.Forms.MessageBox.Show("There was a problem unregistering this dll: \n\"" + ex.Message + "\"");
			}
		}
		#endregion </Unregister the DLL>
		#endregion </SolidWorks Registration>

		#region ISwAddin Implementation
		public SwAddin()
		{
		}

		public bool ConnectToSW(object ThisSW, int cookie)
		{
			iSwApp = (ISldWorks)ThisSW;
			addinID = cookie;

			//Setup callbacks
			iSwApp.SetAddinCallbackInfo(0, this, addinID);

			#region Setup the Event Handlers
			SwEventPtr = (SolidWorks.Interop.sldworks.SldWorks)iSwApp;
			openDocs = new Hashtable();
			AttachEventHandlers();
			#endregion

			return true;
		}

		public bool DisconnectFromSW()
		{
			DetachEventHandlers();

			System.Runtime.InteropServices.Marshal.ReleaseComObject(iSwApp);
			iSwApp = null;
			//The addin _must_ call GC.Collect() here in order to retrieve all managed code pointers 
			GC.Collect();
			GC.WaitForPendingFinalizers();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			return true;
		}
		#endregion

		#region <Event Methods>
		public bool AttachEventHandlers()
		{
			AttachSwEvents();
			//Listen for events on all currently open docs
			AttachEventsToAllDocuments();
			return true;
		}

		private bool AttachSwEvents()
		{
			try
			{
				SwEventPtr.ActiveDocChangeNotify += new DSldWorksEvents_ActiveDocChangeNotifyEventHandler(OnDocChange);
				SwEventPtr.DocumentLoadNotify2 += new DSldWorksEvents_DocumentLoadNotify2EventHandler(OnDocLoad);
				SwEventPtr.FileNewNotify2 += new DSldWorksEvents_FileNewNotify2EventHandler(OnFileNew);
				SwEventPtr.ActiveModelDocChangeNotify += new DSldWorksEvents_ActiveModelDocChangeNotifyEventHandler(OnModelChange);
				SwEventPtr.FileOpenPostNotify += new DSldWorksEvents_FileOpenPostNotifyEventHandler(FileOpenPostNotify);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return false;
			}
		}

		private bool DetachSwEvents()
		{
			try
			{
				SwEventPtr.ActiveDocChangeNotify -= new DSldWorksEvents_ActiveDocChangeNotifyEventHandler(OnDocChange);
				SwEventPtr.DocumentLoadNotify2 -= new DSldWorksEvents_DocumentLoadNotify2EventHandler(OnDocLoad);
				SwEventPtr.FileNewNotify2 -= new DSldWorksEvents_FileNewNotify2EventHandler(OnFileNew);
				SwEventPtr.ActiveModelDocChangeNotify -= new DSldWorksEvents_ActiveModelDocChangeNotifyEventHandler(OnModelChange);
				SwEventPtr.FileOpenPostNotify -= new DSldWorksEvents_FileOpenPostNotifyEventHandler(FileOpenPostNotify);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return false;
			}

		}

		public void AttachEventsToAllDocuments()
		{
			ModelDoc2 modDoc = (ModelDoc2)iSwApp.GetFirstDocument();
			while (modDoc != null)
			{
				if (!openDocs.Contains(modDoc))
				{
					AttachModelDocEventHandler(modDoc);
				}
				modDoc = (ModelDoc2)modDoc.GetNext();
			}
		}

		public bool AttachModelDocEventHandler(ModelDoc2 modDoc)
		{
			if (modDoc == null)
				return false;

			DocumentEventHandler docHandler = null;

			if (!openDocs.Contains(modDoc))
			{
				switch (modDoc.GetType())
				{
					case (int)swDocumentTypes_e.swDocPART:
						{
							docHandler = new PartEventHandler(modDoc, this);
							break;
						}
					case (int)swDocumentTypes_e.swDocASSEMBLY:
						{
							docHandler = new AssemblyEventHandler(modDoc, this);
							break;
						}
					case (int)swDocumentTypes_e.swDocDRAWING:
						{
							docHandler = new DrawingEventHandler(modDoc, this);
							break;
						}
					default:
						{
							return false; //Unsupported document type
						}
				}
				docHandler.AttachEventHandlers();
				openDocs.Add(modDoc, docHandler);
			}
			return true;
		}

		public bool DetachModelEventHandler(ModelDoc2 modDoc)
		{
			DocumentEventHandler docHandler;
			docHandler = (DocumentEventHandler)openDocs[modDoc];
			openDocs.Remove(modDoc);
			modDoc = null;
			docHandler = null;
			return true;
		}

		public bool DetachEventHandlers()
		{
			DetachSwEvents();

			//Close events on all currently open docs
			DocumentEventHandler docHandler;
			int numKeys = openDocs.Count;
			object[] keys = new Object[numKeys];

			//Remove all document event handlers
			openDocs.Keys.CopyTo(keys, 0);
			foreach (ModelDoc2 key in keys)
			{
				docHandler = (DocumentEventHandler)openDocs[key];
				docHandler.DetachEventHandlers(); //This also removes the pair from the hash
				docHandler = null;
			}
			return true;
		}
		#endregion </Event Methods>

		#region Event Handlers
		//Events
		public int OnDocChange()
		{
			return 0;
		}

		public int OnDocLoad(string docTitle, string docPath)
		{
			return 0;
		}

		int FileOpenPostNotify(string FileName)
		{
			AttachEventsToAllDocuments();
			return 0;
		}

		public int OnFileNew(object newDoc, int docType, string templateName)
		{
			AttachEventsToAllDocuments();
			return 0;
		}

		public int OnModelChange()
		{
			return 0;
		}

		#endregion
	}

}
