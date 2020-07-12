using System.Threading;
using System.Windows.Forms;

namespace DXToolKit {
	/// <summary>
	/// Handler for clipboard based operations
	/// Needed since windows requires ApartmentState.STA for application to be able to access the clipboard
	/// </summary>
	public static class ClipboardHandler {
		/// <summary>
		/// Sets input text to the clipboard
		/// </summary>
		/// <param name="text">The text to set to the clipboard</param>
		public static void SetText(string text) {
			var thread = new Thread(() => Clipboard.SetText(text));
			thread.SetApartmentState(ApartmentState.STA); // Set the thread to STA
			thread.Start();
			thread.Join(); // Wait for the thread to end
		}

		/// <summary>
		/// Gets the current text from the clipboard
		/// </summary>
		/// <returns>The text held by the clipboard</returns>
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