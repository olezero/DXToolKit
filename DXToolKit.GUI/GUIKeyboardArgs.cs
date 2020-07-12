using System.Collections.Generic;
using System.Windows.Forms;
using SharpDX.DirectInput;

namespace DXToolKit.GUI {
	public class GUIKeyboardArgs {
		/// <summary>
		/// List containing all keys that are pressed
		/// </summary>
		public List<Key> KeysPressed;

		/// <summary>
		/// List containing all keys that are in a up state this frame
		/// </summary>
		public List<Key> KeysUp;

		/// <summary>
		/// List containing all keys that are in a down state this frame
		/// </summary>
		public List<Key> KeysDown;

		/// <summary>
		/// Text input string for this frame
		/// </summary>
		public string TextInput;

		/// <summary>
		/// Repeating key, set to null if there is no key this frame
		/// </summary>
		public Key? RepeatKey;
	}
}