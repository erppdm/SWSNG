using System;
using System.Windows.Forms;

namespace SWSNG
{
	class SWSNGSelectNoRange
	{
		public string serialNumber { get; private set; }

		public SWSNGSelectNoRange(int hWnd)
		{
			serialNumber = string.Empty;
			FrmSWSNGSelectNoRange frm = new FrmSWSNGSelectNoRange();
			DialogResult dR = frm.ShowDialog(Control.FromHandle((IntPtr)hWnd));
			serialNumber = frm.serialNo;
		}
	}
}
