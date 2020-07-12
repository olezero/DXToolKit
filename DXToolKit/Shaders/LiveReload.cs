using System;
using System.IO;

namespace DXToolKit {
	/// <summary>
	/// Basic wrapper for file system watcher class.
	/// </summary>
	public static class LiveReload {
		/// <summary>
		/// Creates a file system watcher to watch for changes in the input file.
		/// </summary>
		/// <param name="filename">The file to watch</param>
		/// <param name="OnReload">Action called 100ms after the file has changed</param>
		/// <param name="runimmediatly">If OnReload should be called once</param>
		/// <exception cref="TimeoutException">Will throw an exception if the target file does not become readable within 1 second</exception>
		/// <returns>The watcher</returns>
		public static FileSystemWatcher CreateWatcher(string filename, Action<string> OnReload, bool runimmediatly = false) {
			// Split file and directory
			var file = filename.Substring(filename.LastIndexOf('\\') + 1);
			var dir = filename.Substring(0, filename.LastIndexOf('\\'));
			// Create a watcher that looks at the directory, with a filter of the filename
			var watcher = new FileSystemWatcher(dir, file) {
				// Using last access since it only pushes a single event for when a file is updated
				NotifyFilter = NotifyFilters.LastWrite,
				// Enable raising of events is a good idea.
				EnableRaisingEvents = true,
			};


			watcher.Changed += (sender, args) => {
				// Could just use watcher directly, but makes more sense to use the argument to the function.
				var fsWatcher = (FileSystemWatcher) sender;

				// Disable event raising since we are compiling
				fsWatcher.EnableRaisingEvents = false;

				// Create timeout counter.
				var timeoutCounter = 0;

				// Wait for the file to be ready
				while (!IsFileReady(filename)) {
					// Wait for one ms
					System.Threading.Thread.Sleep(1);
					// Increment timeout counter
					timeoutCounter++;
					// If waiting exceeds 1000 ms, throw timeout exception
					if (timeoutCounter > 1000) {
						throw new TimeoutException();
					}
				}

				// Call reload function
				OnReload?.Invoke(filename);

				// Enable event raising again
				fsWatcher.EnableRaisingEvents = true;
			};


			// If run once, call on reload immediately
			if (runimmediatly) {
				OnReload?.Invoke(filename);
			}

			// Return the file system watcher
			return watcher;
		}

		/// <summary>
		/// Checks if a file is readable
		/// </summary>
		/// <param name="filename">The file to check</param>
		/// <returns>Value indicating if the file is readable</returns>
		public static bool IsFileReady(string filename) {
			// If the file can be opened for exclusive access it means that the file
			// is no longer locked by another process.
			try {
				using (var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
					return inputStream.Length > 0;
			}
			catch (Exception) {
				return false;
			}
		}
	}
}