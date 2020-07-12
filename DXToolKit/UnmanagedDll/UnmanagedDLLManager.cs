using System;
using System.IO;

namespace DXToolKit.UnmanagedDll {
	public static class UnmanagedDLLManager {
		public static void Unpack() {
			var assembly = typeof(UnmanagedDLLManager).Assembly;
			var embeddedFiles = assembly.GetManifestResourceNames();
			foreach (var embeddedFile in embeddedFiles) {
				// Find all files inside unmanaged dll folder in embedded data
				// Remove namespace
				var filename = embeddedFile.Replace("DXToolKit.UnmanagedDll.", "");
				// Check if file does not exist
				if (File.Exists(filename) == false) {
					// Get manifest stream for that file
					using (var stream = assembly.GetManifestResourceStream(embeddedFile)) {
						// Open a file stream to write contents
						using (var file = new FileStream(filename, FileMode.Create, FileAccess.Write)) {
							if (stream == null) throw new NullReferenceException();
							stream.CopyTo(file);
						}
					}
				}
			}
		}
	}
}