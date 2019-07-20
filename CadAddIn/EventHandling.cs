using System.Collections;
using SolidWorks.Interop.sldworks;

namespace SWSNG
{
	public class DocumentEventHandler
	{
		protected ISldWorks iSwApp;
		protected ModelDoc2 document;
		protected SwAddin userAddin;

		protected Hashtable openModelViews;

		public DocumentEventHandler(ModelDoc2 modDoc, SwAddin addin)
		{
			document = modDoc;
			userAddin = addin;
			iSwApp = (ISldWorks)userAddin.SwApp;
			openModelViews = new Hashtable();
		}

		virtual public bool AttachEventHandlers()
		{ return true; }

		virtual public bool DetachEventHandlers()
		{ return true; }

		public bool ConnectModelViews()
		{
			IModelView mView;
			mView = (IModelView)document.GetFirstModelView();

			while (mView != null)
			{
				if (!openModelViews.Contains(mView))
				{
					DocView dView = new DocView(userAddin, mView, this);
					dView.AttachEventHandlers();
					openModelViews.Add(mView, dView);
				}
				mView = (IModelView)mView.GetNext();
			}
			return true;
		}

		public bool DisconnectModelViews()
		{
			//Close events on all currently open docs
			DocView dView;
			int numKeys;
			numKeys = openModelViews.Count;

			if (numKeys == 0)
			{ return false; }

			object[] keys = new object[numKeys];

			//Remove all ModelView event handlers
			openModelViews.Keys.CopyTo(keys, 0);
			foreach (ModelView key in keys)
			{
				dView = (DocView)openModelViews[key];
				dView.DetachEventHandlers();
				openModelViews.Remove(key);
				dView = null;
			}
			return true;
		}

		public bool DetachModelViewEventHandler(ModelView mView)
		{
			DocView dView;
			if (openModelViews.Contains(mView))
			{
				dView = (DocView)openModelViews[mView];
				openModelViews.Remove(mView);
				mView = null;
				dView = null;
			}
			return true;
		}
	}

	public class PartEventHandler : DocumentEventHandler
	{
		PartDoc doc;

		public PartEventHandler(ModelDoc2 modDoc, SwAddin addin) : base(modDoc, addin)
		{ doc = (PartDoc)document; }

		override public bool AttachEventHandlers()
		{
			doc.DestroyNotify += new DPartDocEvents_DestroyNotifyEventHandler(OnDestroy);
			doc.NewSelectionNotify += new DPartDocEvents_NewSelectionNotifyEventHandler(OnNewSelection);
			#region <BiI customised>
			doc.FileSaveAsNotify2 += new DPartDocEvents_FileSaveAsNotify2EventHandler(OnFileSaveAsNotify2);
			#endregion </BiI customised>
			ConnectModelViews();

			return true;
		}

		override public bool DetachEventHandlers()
		{
			doc.DestroyNotify -= new DPartDocEvents_DestroyNotifyEventHandler(OnDestroy);
			doc.NewSelectionNotify -= new DPartDocEvents_NewSelectionNotifyEventHandler(OnNewSelection);
			#region <BiI customised>
			doc.FileSaveAsNotify2 -= new DPartDocEvents_FileSaveAsNotify2EventHandler(OnFileSaveAsNotify2);
			#endregion </BiI customised>
			DisconnectModelViews();

			userAddin.DetachModelEventHandler(document);
			return true;
		}

		//Event Handlers
		public int OnDestroy()
		{
			DetachEventHandlers();
			return 0;
		}

		public int OnNewSelection()
		{ return 0; }

		#region <BiI customised>
		public int OnFileSaveAsNotify2(string fileName)
		{
			return swsng(fileName);
		}

		private int swsng(string fileName)
		{
			if (SWSNG.swsngParts)
			{
				string newFileName = string.Empty;
				newFileName = SWSNG.GetSerialNo(ref this.iSwApp);
				if (SWSNG.errCode != 0)
				{
					// Your error handling
					return 1;
				}
				if (newFileName == string.Empty)
				{
					// Your handling if the user cancels the selection
					return 1;
				}
				SWSNG.SetNewFileName setFileName = new SWSNG.SetNewFileName(iSwApp.GetProcessID(), newFileName + System.IO.Path.GetExtension(fileName));
				if (SWSNG.errCode != 0)
				{
					// Your error handling
					return 1;
				}
				return 0;
			}
			return 0;
		}
		#endregion </BiI customised>
	}

	public class AssemblyEventHandler : DocumentEventHandler
	{
		AssemblyDoc doc;

		public AssemblyEventHandler(ModelDoc2 modDoc, SwAddin addin) : base(modDoc, addin)
		{ doc = (AssemblyDoc)document; }

		override public bool AttachEventHandlers()
		{
			doc.DestroyNotify += new DAssemblyDocEvents_DestroyNotifyEventHandler(OnDestroy);
			doc.NewSelectionNotify += new DAssemblyDocEvents_NewSelectionNotifyEventHandler(OnNewSelection);
			#region <BiI customised>
			doc.FileSaveAsNotify2 += new DAssemblyDocEvents_FileSaveAsNotify2EventHandler(OnFileSaveAsNotify2);
			#endregion </BiI customised>

			ConnectModelViews();

			return true;
		}

		override public bool DetachEventHandlers()
		{
			doc.DestroyNotify -= new DAssemblyDocEvents_DestroyNotifyEventHandler(OnDestroy);
			doc.NewSelectionNotify -= new DAssemblyDocEvents_NewSelectionNotifyEventHandler(OnNewSelection);
			#region <BiI customised>
			doc.FileSaveAsNotify2 -= new DAssemblyDocEvents_FileSaveAsNotify2EventHandler(OnFileSaveAsNotify2);
			#endregion </BiI customised>
			DisconnectModelViews();

			userAddin.DetachModelEventHandler(document);
			return true;
		}

		//Event Handlers
		public int OnDestroy()
		{
			DetachEventHandlers();
			return 0;
		}

		public int OnNewSelection()
		{ return 0; }

		#region <BiI customised>
		public int OnFileSaveAsNotify2(string fileName)
		{
			return swsng(fileName);
		}

		private int swsng(string fileName)
		{
			if (SWSNG.swsngAssemblies)
			{
				string newFileName = string.Empty;
				newFileName = SWSNG.GetSerialNo(ref this.iSwApp);
				if (SWSNG.errCode != 0)
				{
					// Your error handling
					return 1;
				}
				if (newFileName == string.Empty)
				{
					// Your handling if the user cancels the selection
					return 1;
				}
				SWSNG.SetNewFileName setFileName = new SWSNG.SetNewFileName(iSwApp.GetProcessID(), newFileName + System.IO.Path.GetExtension(fileName));
				if (SWSNG.errCode != 0)
				{
					// Your error handling
					return 1;
				}
				return 0;
			}
			return 0;
		}
		#endregion </BiI customised>
	}

	public class DrawingEventHandler : DocumentEventHandler
	{
		DrawingDoc doc;

		public DrawingEventHandler(ModelDoc2 modDoc, SwAddin addin) : base(modDoc, addin)
		{ doc = (DrawingDoc)document; }

		override public bool AttachEventHandlers()
		{
			doc.DestroyNotify += new DDrawingDocEvents_DestroyNotifyEventHandler(OnDestroy);
			doc.NewSelectionNotify += new DDrawingDocEvents_NewSelectionNotifyEventHandler(OnNewSelection);
			#region <BiI customised>
			doc.FileSaveAsNotify2 += new DDrawingDocEvents_FileSaveAsNotify2EventHandler(OnFileSaveAsNotify2);
			#endregion </BiI customised>
			ConnectModelViews();

			return true;
		}

		override public bool DetachEventHandlers()
		{
			doc.DestroyNotify -= new DDrawingDocEvents_DestroyNotifyEventHandler(OnDestroy);
			doc.NewSelectionNotify -= new DDrawingDocEvents_NewSelectionNotifyEventHandler(OnNewSelection);
			#region <BiI customised>
			doc.FileSaveAsNotify2 -= new DDrawingDocEvents_FileSaveAsNotify2EventHandler(OnFileSaveAsNotify2);
			#endregion </BiI customised>
			DisconnectModelViews();

			userAddin.DetachModelEventHandler(document);
			return true;
		}

		//Event Handlers
		public int OnDestroy()
		{
			DetachEventHandlers();
			return 0;
		}

		public int OnNewSelection()
		{ return 0; }

		#region <BiI customised>
		public int OnFileSaveAsNotify2(string fileName)
		{
			return swsng(fileName);
		}

		private int swsng(string fileName)
		{
			if (SWSNG.swsngDrawings)
			{
				string newFileName = string.Empty;
				newFileName = SWSNG.GetSerialNo(ref this.iSwApp);
				if (SWSNG.errCode != 0)
				{
					// Your error handling
					return 1;
				}
				if (newFileName == string.Empty)
				{
					// Your handling if the user cancels the selection
					return 1;
				}
				SWSNG.SetNewFileName setFileName = new SWSNG.SetNewFileName(iSwApp.GetProcessID(), newFileName + System.IO.Path.GetExtension(fileName));
				if (SWSNG.errCode != 0)
				{
					// Your error handling
					return 1;
				}
				return 0;
			}
			return 0;
		}
		#endregion </BiI customised>

	}

	public class DocView
	{
		ISldWorks iSwApp;
		SwAddin userAddin;
		ModelView mView;
		DocumentEventHandler parent;

		public DocView(SwAddin addin, IModelView mv, DocumentEventHandler doc)
		{
			userAddin = addin;
			mView = (ModelView)mv;
			iSwApp = (ISldWorks)userAddin.SwApp;
			parent = doc;
		}

		public bool AttachEventHandlers()
		{
			mView.DestroyNotify2 += new DModelViewEvents_DestroyNotify2EventHandler(OnDestroy);
			mView.RepaintNotify += new DModelViewEvents_RepaintNotifyEventHandler(OnRepaint);
			return true;
		}

		public bool DetachEventHandlers()
		{
			mView.DestroyNotify2 -= new DModelViewEvents_DestroyNotify2EventHandler(OnDestroy);
			mView.RepaintNotify -= new DModelViewEvents_RepaintNotifyEventHandler(OnRepaint);
			parent.DetachModelViewEventHandler(mView);
			return true;
		}

		//EventHandlers
		public int OnDestroy(int destroyType)
		{ return 0; }

		public int OnRepaint(int repaintType)
		{ return 0; }
	}
}
