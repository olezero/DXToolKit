using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DXToolKit.Engine {
	/// <summary>
	/// Simple file browser
	/// </summary>
	public class FileBrowser : GUIElement {
		private class FilebrowserOption : ListboxOption {
			public readonly string Fullpath;
			public readonly bool IsFile;

			public FilebrowserOption(string name, string fullpath, bool isFile) : base(name) {
				Fullpath = fullpath;
				IsFile = isFile;
			}
		}

		/// <summary>
		/// Shows a path to the current directory
		/// </summary>
		private Combobox<FilebrowserOption> m_driveSelector;

		/// <summary>
		/// Contains a list of all files and directories at the current path
		/// </summary>
		private Listbox<FilebrowserOption> m_fileList;


		/// <summary>
		/// Invoked when a directory is selected
		/// </summary>
		public event Action<string> DirectorySelected;

		/// <summary>
		/// Invoked when a file is selected
		/// </summary>
		public event Action<string> FileSelected;

		/// <summary>
		/// Invoked when a directory is selected
		/// </summary>
		protected virtual void OnDirectiorySelected(string directory) => DirectorySelected?.Invoke(directory);

		/// <summary>
		/// Invoked when a file is selected
		/// </summary>
		protected virtual void OnFileSelected(string file) => FileSelected?.Invoke(file);

		/// <summary>
		/// Actual path the browser is pointing to
		/// </summary>
		private string m_currentPath;

		/// <summary>
		/// Filter used when fetching files
		/// </summary>
		//private string m_searchPattern;
		private string[] m_searchPatterns;

		/// <summary>
		/// Creates a new file browser
		/// </summary>
		public FileBrowser(string startingPath, string searchPattern = "*") : this(startingPath, new[] {searchPattern}) { }

		/// <summary>
		/// Creates a new file browser
		/// </summary>
		public FileBrowser(string startingPath, string[] searchPatterns) {
			m_currentPath = startingPath;
			m_searchPatterns = searchPatterns;
			m_fileList = Append(new Listbox<FilebrowserOption> {
				UseDynamicHeight = false,
				ShineOpacity = 0.0F,
			});
			m_driveSelector = Append(new Combobox<FilebrowserOption> {
				Text = startingPath,
			});

			foreach (var drive in Directory.GetLogicalDrives()) {
				m_driveSelector.ComboList.AddOption(new FilebrowserOption(drive, drive, false));
			}

			Width = 256;
			Height = 256;
			PopulateBrowser();

			m_driveSelector.OptionClicked += BrowseTo;
			m_fileList.OptionDoubleClicked += BrowseTo;
		}


		private void BrowseTo(FilebrowserOption option) {
			if (option.IsFile == false) {
				m_currentPath = option.Fullpath;
				m_driveSelector.Text = m_currentPath;
				PopulateBrowser();
				m_fileList.ScrollToTop();
				OnDirectiorySelected(option.Fullpath);
			} else {
				OnFileSelected(option.Fullpath);
			}
		}

		/// <summary>
		/// Populates the file list with all files in current path
		/// </summary>
		private void PopulateBrowser() {
			try {
				m_fileList.RemoveAllOptions();

				var directories = Directory.GetDirectories(m_currentPath);
				var files = new List<string>();
				for (int i = 0; i < m_searchPatterns.Length; i++) {
					files.AddRange(Directory.GetFiles(m_currentPath, m_searchPatterns[i]));
				}

				files = files.Distinct().ToList();

				var previousDirectory = Path.GetDirectoryName(m_currentPath);
				if (previousDirectory != null) {
					m_fileList.AddOption(new FilebrowserOption(".", previousDirectory, false));
				}

				for (int i = 0; i < directories.Length; i++) {
					// Dir skipping volume info
					var dir = directories[i].Substring(3);

					// Need to trim from last /
					if (dir.Contains(Path.DirectorySeparatorChar.ToString())) {
						dir = dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1);
					}

					// Remove any folder that starts with a $
					if (dir.StartsWith("$")) {
						continue;
					}

					// Try getting files from directories, fails if no access or folder cannot be read
					try {
						Directory.GetFiles(directories[i]);
					}
					catch (Exception) {
						continue;
					}

					m_fileList.AddOption(new FilebrowserOption(dir, directories[i], false));
				}

				for (int i = 0; i < files.Count; i++) {
					var path = files[i];
					var filename = Path.GetFileName(path);
					m_fileList.AddOption(new FilebrowserOption(filename, path, true));
				}
			}
			catch (Exception e) {
				Debug.Log(e, 1000);
			}
		}


		/// <inheritdoc />
		protected override void PostRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			tools.Background.BevelBorder();
		}

		/// <inheritdoc />
		protected override void OnBoundsChangedDirect() {
			PositionChildren();
			base.OnBoundsChangedDirect();
		}

		private void PositionChildren() {
			m_driveSelector.Width = Width;
			m_fileList.Width = Width;
			m_fileList.Height = Height - m_driveSelector.Height;
			m_fileList.Top = m_driveSelector.Bottom;
		}
	}
}