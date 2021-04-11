using System;

namespace DXToolKit.Engine {
	/// <summary>
	/// Simple open file dialog window
	/// </summary>
	public class OpenFileDialog : Window {
		private FileBrowser m_fileBrowser;

		/// <summary>
		/// Gets a reference to the file browser used by the dialog
		/// </summary>
		public FileBrowser FileBrowser => m_fileBrowser;

		/// <summary>
		/// Gets or sets a value indicating if the dialog should be closed after a file is selected
		/// </summary>
		public bool CloseOnSelect = true;

		/// <summary>
		/// Event invoked when a file is selected
		/// </summary>
		public event Action<string> FileSelected;

		/// <summary>
		/// Creates a new file dialog
		/// </summary>
		/// <param name="startingPath">Start path to point to</param>
		/// <param name="filter">Search filter for files</param>
		public OpenFileDialog(string startingPath, string filter = "*") : this(startingPath, new[] {filter}) { }

		/// <summary>
		/// Creates a new file dialog
		/// </summary>
		/// <param name="startingPath">Start path to point to</param>
		/// <param name="filters">Search filter for files</param>
		public OpenFileDialog(string startingPath, string[] filters) {
			Text = "Open File";
			m_fileBrowser = Body.Append(new FileBrowser(startingPath, filters));
			Width = 256 * 1.5F;
			Height = 256;
			m_fileBrowser.FileSelected += file => {
				OnFileSelected(file);
				if (CloseOnSelect) Close();
			};
			PositionChildren();
		}

		/// <summary>
		/// Invoked when a file is selected
		/// </summary>
		/// <param name="file">The path to the selected file</param>
		public virtual void OnFileSelected(string file) => FileSelected?.Invoke(file);

		/// <inheritdoc />
		protected override void OnBoundsChangedDirect() {
			PositionChildren();
			base.OnBoundsChangedDirect();
		}

		private void PositionChildren() {
			if (m_fileBrowser != null) {
				m_fileBrowser.X = 0;
				m_fileBrowser.Y = 0;
				m_fileBrowser.Width = Body.Width;
				m_fileBrowser.Height = Body.Height;
			}
		}
	}
}