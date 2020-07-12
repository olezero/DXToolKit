using System.Threading;
using System.Windows.Forms;

namespace DXToolKit {
	public static class ClipboardHandler {
		public static void SetText(string text) {
			var thread = new Thread(() => Clipboard.SetText(text));
			thread.SetApartmentState(ApartmentState.STA); // Set the thread to STA
			thread.Start();
			thread.Join(); // Wait for the thread to end
		}

		public static string GetText() {
			var result = "";

			var thread = new Thread(() => result = Clipboard.GetText());
			thread.SetApartmentState(ApartmentState.STA); // Set the thread to STA
			thread.Start();
			thread.Join(); // Wait for the thread to end

			return result;
		}
	}
}