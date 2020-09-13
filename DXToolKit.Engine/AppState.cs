using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;

namespace DXToolKit.Engine {
	/// <summary>
	/// Key/Value state manager
	/// </summary>
	public static class AppState {
		/// <summary>
		/// Gets or sets a value indicating if the state should throw errors if value is not found
		/// </summary>
		public static bool THROW_ERROR_ON_NOT_FOUND = false;

		/// <summary>
		/// Gets or sets a value indicating if the state should throw errors if trying to get a value of the wrong type
		/// </summary>
		public static bool THROW_ERROR_ON_TYPE_MISMATCH = true;

		/// <summary>
		/// If state save should be compressed
		/// </summary>
		public static bool COMPRESS_STATE = true;

		/// <summary>
		/// Event handler for state changes
		/// </summary>
		/// <param name="key">State key</param>
		/// <param name="value">State value</param>
		public delegate void StateEventHandler(object key, object value);

		/// <summary>
		/// Invoked when a new key is set in the state
		/// </summary>
		public static event StateEventHandler OnStateSet;

		/// <summary>
		/// Invoked when a value is changed
		/// </summary>
		public static event StateEventHandler OnStateChanged;

		/// <summary>
		/// Invoked when a key is removed from the state
		/// </summary>
		public static event StateEventHandler OnStateRemoved;

		/// <summary>
		/// Storage of the actual state
		/// </summary>
		private static Dictionary<object, object> m_state = new Dictionary<object, object>();

		/// <summary>
		/// Clears all data from the state
		/// </summary>
		public static void ClearState() {
			foreach (var keyValue in m_state) {
				OnStateRemoved?.Invoke(keyValue.Key, keyValue.Value);
			}

			m_state.Clear();
		}

		/// <summary>
		/// Saves the state to a file
		/// Only serializable objects in the state manager will be saved
		/// </summary>
		/// <param name="filename">File to save state to</param>
		public static void Save(string filename) {
			try {
				var formatter = new BinaryFormatter();
				using (var memStream = new MemoryStream()) {
					formatter.Serialize(memStream, m_state);
					if (COMPRESS_STATE) {
						var compressed = Compress(memStream.ToArray());
						File.WriteAllBytes(filename, compressed);
					} else {
						File.WriteAllBytes(filename, memStream.ToArray());
					}
				}
			}
			catch (Exception) {
				// ignored
			}
		}

		/// <summary>
		/// Loads state from a given filename
		/// </summary>
		/// <param name="filename">File to load state from</param>
		public static void Load(string filename) {
			try {
				if (File.Exists(filename)) {
					var data = File.ReadAllBytes(filename);

					if (COMPRESS_STATE) {
						var decomp = Decompress(data);
						using (var memstream = new MemoryStream(decomp)) {
							var formatter = new BinaryFormatter();
							m_state = (Dictionary<object, object>) formatter.Deserialize(memstream);
						}
					} else {
						using (var memstream = new MemoryStream(data)) {
							var formatter = new BinaryFormatter();
							m_state = (Dictionary<object, object>) formatter.Deserialize(memstream);
						}
					}
				}
			}
			catch (Exception) {
				// ignored
			}
		}

		/// <summary>
		/// Gets a value from the state as an object
		/// </summary>
		/// <param name="key">The key of the value</param>
		/// <returns>The value</returns>
		public static object GetValue(object key) {
			if (m_state.ContainsKey(key)) {
				return m_state[key];
			}

			if (THROW_ERROR_ON_NOT_FOUND) {
				throw new KeyNotFoundException($"{key} not found in state");
			}

			return null;
		}


		/// <summary>
		/// Gets a value from the state as a bool
		/// </summary>
		/// <param name="key">The key of the value</param>
		/// <param name="defaultValue">Default value if key is not found</param>
		/// <returns>The value or false</returns>
		public static bool GetValueAsBool(object key, bool defaultValue = false) {
			return GetValue(key, defaultValue);
		}

		/// <summary>
		/// Gets a value from the state as a int
		/// </summary>
		/// <param name="key">The key of the value</param>
		/// <param name="defaultValue">Default value if key is not found</param>
		/// <returns>The value or 0</returns>
		public static int GetValueAsInt(object key, int defaultValue = 0) {
			return GetValue(key, defaultValue);
		}

		/// <summary>
		/// Gets a value from the state as a float
		/// </summary>
		/// <param name="key">The key of the value</param>
		/// <param name="defaultValue">Default value if key is not found</param>
		/// <returns>The value or 0.0F</returns>
		public static float GetValueAsFloat(object key, float defaultValue = 0.0F) {
			return GetValue(key, defaultValue);
		}

		/// <summary>
		/// Gets a value from the state as a double
		/// </summary>
		/// <param name="key">The key of the value</param>
		/// <param name="defaultValue">Default value if key is not found</param>
		/// <returns>The value or 0.0</returns>
		public static double GetValueAsDouble(object key, double defaultValue = 0.0) {
			return GetValue(key, defaultValue);
		}

		/// <summary>
		/// Gets a value from the state as an string
		/// </summary>
		/// <param name="key">The key of the value</param>
		/// <param name="defaultValue">Default value if key is not found</param>
		/// <returns>The value or default(string)</returns>
		public static string GetValueAsString(object key, string defaultValue = null) {
			return GetValue(key, defaultValue);
		}

		/// <summary>
		/// Gets a value from the state as T
		/// </summary>
		/// <param name="key">The key of the value</param>
		/// <param name="defaultValue">A default value to return if state is not found</param>
		/// <returns>The value or default(T)</returns>
		public static T GetValue<T>(object key, T defaultValue = default) {
			if (m_state.ContainsKey(key)) {
				var val = m_state[key];
				if (val is T ttype) {
					return ttype;
				}

				if (THROW_ERROR_ON_TYPE_MISMATCH) {
					throw new InvalidCastException($"value was not of type {typeof(T)}");
				}
			}

			if (THROW_ERROR_ON_NOT_FOUND) {
				throw new KeyNotFoundException($"{key} not found in state");
			}

			return defaultValue;
		}

		/// <summary>
		/// Sets a value in the state
		/// </summary>
		/// <param name="key">The key to the value</param>
		/// <param name="value">The value</param>
		/// <returns>The value</returns>
		public static object SetValue(object key, object value) {
			if (m_state.ContainsKey(key) == false) {
				m_state.Add(key, value);
				OnStateSet?.Invoke(key, value);
			} else {
				m_state[key] = value;
			}

			OnStateChanged?.Invoke(key, value);
			return value;
		}

		/// <summary>
		/// Sets a value in the state
		/// </summary>
		/// <param name="key">The key to the value</param>
		/// <param name="value">The value</param>
		/// <returns>The value</returns>
		public static T SetValue<T>(object key, T value) {
			if (m_state.ContainsKey(key) == false) {
				m_state.Add(key, value);
				OnStateSet?.Invoke(key, value);
			} else {
				m_state[key] = value;
			}

			OnStateChanged?.Invoke(key, value);
			return value;
		}

		/// <summary>
		/// Deletes a value by key
		/// </summary>
		/// <param name="key">Key of the value to remove</param>
		/// <returns>The removed value</returns>
		public static object RemoveValue(object key) {
			return RemoveValue<object>(key);
		}

		/// <summary>
		/// Deletes a value by key
		/// </summary>
		/// <param name="key">Key of the value to remove</param>
		/// <returns>The removed value</returns>
		/// <exception cref="InvalidCastException">Exception if input T does not match the target type</exception>
		/// <exception cref="KeyNotFoundException">Exception if input key is not found</exception>
		public static T RemoveValue<T>(object key) {
			if (m_state.ContainsKey(key)) {
				var val = m_state[key];
				m_state.Remove(key);
				OnStateRemoved?.Invoke(key, val);
				if (val is T ttype) {
					return ttype;
				}

				if (THROW_ERROR_ON_TYPE_MISMATCH) {
					throw new InvalidCastException($"value was not of type {typeof(T)}");
				}
			}

			if (THROW_ERROR_ON_NOT_FOUND) {
				throw new KeyNotFoundException($"{key} not found in state");
			}

			return default;
		}


		private static byte[] Compress(byte[] data) {
			var output = new MemoryStream();
			using (var dstream = new DeflateStream(output, CompressionLevel.Optimal)) {
				dstream.Write(data, 0, data.Length);
			}

			return output.ToArray();
		}

		private static byte[] Decompress(byte[] data) {
			var input = new MemoryStream(data);
			var output = new MemoryStream();
			using (var dstream = new DeflateStream(input, CompressionMode.Decompress)) {
				dstream.CopyTo(output);
			}

			return output.ToArray();
		}
	}
}