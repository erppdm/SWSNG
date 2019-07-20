using SolidWorks.Interop.sldworks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System;

namespace SWSNG
{
	public class SWSNG
	{
		#region <Specifies which file types SWSNG use>
		public static readonly bool swsngParts = true;
		public static readonly bool swsngAssemblies = false;
		public static readonly bool swsngDrawings = false;
		#endregion </Specifies which file types SWSNG use>

		public static int errCode;
		public static string errMsg;

		public static string GetSerialNo(ref ISldWorks iSwApp)
		{
			SWSNG.errCode = 0;
			SWSNG.errMsg = string.Empty;

			SWSNGSelectNoRange dBSNo = new SWSNGSelectNoRange(iSwApp.GetProcessID());
			return dBSNo.serialNumber;
		}

		public class SetNewFileName
		{
			public static int hWndSldWorks { get; private set; }
			public static int hWndFileSaveWindow { get; private set; }
			public static int hWndFileName { get; private set; }
			public static string strFileName { get; private set; }
			private static DateTime startTime;
			private static int timeOut = 10;

			[DllImport("user32.dll")]
			private static extern bool EnumWindows(_EnumWindows _enumWindows, IntPtr lParam);
			private delegate bool _EnumWindows(int hWnd, IntPtr lParam);
			[DllImport("User32.dll")]
			static extern bool EnumChildWindows(int hWndParent, Delegate lpEnumFunc, int lParam);
			public delegate int _EnumChildWindows(int hWnd, int lParam);

			[DllImport("user32.dll")]
			private static extern int GetParent(int hWnd);
			[DllImport("user32.dll", CharSet = CharSet.Unicode)]
			private static extern int GetWindowText(int hWnd, StringBuilder strText, int maxCount);
			[DllImport("user32.dll", CharSet = CharSet.Unicode)]
			private static extern int GetWindowTextLength(int hWnd);
			[DllImport("user32.dll")]
			private static extern bool IsWindowVisible(int hWnd);
			[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
			private static extern int GetClassName(int hWnd, StringBuilder lpClassName, int nMaxCount);

			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			private static extern int SendMessage(int hWnd, int Msg, int wParam, StringBuilder lParam);
			private BackgroundWorker bGWorker = new BackgroundWorker();

			private static bool FindSaveAsDialog(int hWnd, IntPtr lParam)
			{
				int size = GetWindowTextLength(hWnd);
				if (size++ > 0 && IsWindowVisible(hWnd))
				{
					StringBuilder sb = new StringBuilder(size);
					GetWindowText(hWnd, sb, size);
					if (sb.ToString().ToLower() == @"save as")
					{
						if (GetParent(hWnd) == hWndSldWorks)
						{
							hWndFileSaveWindow = (int)hWnd;
							return false;
						}
					}
				}
				return true;
			}

			private static int SetValueEditControl(int hWnd, int lParam)
			{
				StringBuilder className = new StringBuilder(32);
				GetClassName(hWnd, className, className.Capacity);
				if (className.ToString().ToLower().Equals("edit") && IsWindowVisible(hWnd))
				{
					int hWndParent = GetParent(hWnd);
					GetClassName(hWndParent, className, className.Capacity);
					if (className.ToString().ToLower() == "combobox")
					{
						hWndFileName = (int)hWnd;
						SendMessage(hWndFileName, 0xC, 0, new StringBuilder(strFileName));
						return 0;
					}
				}
				return 1;
			}

			public SetNewFileName(int pid, string fileName)
			{
				if (SWSNG.errCode == 0)
				{
					strFileName = fileName;
					hWndSldWorks = (int)Process.GetProcessById(pid).MainWindowHandle;
					hWndFileSaveWindow = 0;
					hWndFileName = 0;
					bGWorker.WorkerSupportsCancellation = true;
					bGWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bGWorker_DoWork);
					bGWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.bGWorker_Completed);
					bGWorker.RunWorkerAsync();
				}
			}

			~SetNewFileName()
			{ }

			private void StartTask(BackgroundWorker bw)
			{
				startTime = DateTime.Now;
				Task task = new Task(() =>
				{
					int counter = 0;
					bool exit = false;
					do
					{
						counter++;
						if (counter % 1 == 0)
						{
							if (startTime.AddSeconds(timeOut) <= DateTime.Now)
							{
								bw.CancelAsync();
								exit = true;
							}
							Thread.Sleep(50);
						}
						if (!exit)
						{
							EnumWindows(new _EnumWindows(FindSaveAsDialog), IntPtr.Zero);
							if (hWndFileSaveWindow != 0)
							{
								EnumChildWindows(hWndFileSaveWindow, new _EnumChildWindows(SetValueEditControl), 0);
								exit = true;
							}
						}
					} while (!exit);
				});
				task.Start();
				task.Wait();
				task.Dispose();
			}

			private void bGWorker_DoWork(object sender, DoWorkEventArgs e)
			{
				BackgroundWorker bw = sender as BackgroundWorker;
				StartTask(bw);
				if (bw.CancellationPending)
				{ e.Cancel = true; }
			}

			private void bGWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
			{
				if (e.Cancelled)
				{
					// Timeout during the search for the Save as dialogue
					SWSNG.errCode = 300;
					SWSNG.errMsg = @"Timeout";
				}
				else if (e.Error != null)
				{
					// There was an error during the operation
					SWSNG.errCode = 301;
					SWSNG.errMsg = e.Error.Message;
				}
				else
				{
					// The operation completed normally
				}
			}
		}
	}
}
