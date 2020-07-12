using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpDX.Windows;

namespace DXToolKit.Engine {
	public class QuietRenderForm : RenderForm {
		[DllImport("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		public QuietRenderForm() {
			WindowState = FormWindowState.Minimized;
			FormBorderStyle = FormBorderStyle.None;
			MouseDown += (sender, args) => {
				this.Focus();
				SetForegroundWindow(this.Handle);
			};
		}

		protected override bool ShowWithoutActivation => true;

		protected override CreateParams CreateParams {
			get {
				CreateParams baseParams = base.CreateParams;

				const int WS_EX_NOACTIVATE = 0x08000000;
				const int WS_EX_TOOLWINDOW = 0x00000080;
				baseParams.ExStyle |= (int) (WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);

				return baseParams;
			}
		}
	}
}