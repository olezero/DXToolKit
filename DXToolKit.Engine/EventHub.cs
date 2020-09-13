using System;
using System.Collections.Generic;

namespace DXToolKit.Engine {
	/// <summary>
	/// Event hub used to handle application wide events
	/// </summary>
	public static class EventHub {
		private static Dictionary<object, List<Action>> m_actions = new Dictionary<object, List<Action>>();
		private static Dictionary<object, List<Action<object[]>>> m_parameterActions = new Dictionary<object, List<Action<object[]>>>();

		/// <summary>
		/// Invokes a given event by key
		/// </summary>
		/// <param name="key">The event to invoke</param>
		/// <param name="args">Arguments that should be passed to the event</param>
		public static void Invoke(object key, params object[] args) {
			if (m_parameterActions.ContainsKey(key)) {
				foreach (var action in m_parameterActions[key]) {
					action?.Invoke(args);
				}
			}

			if (m_actions.ContainsKey(key)) {
				foreach (var action in m_actions[key]) {
					action?.Invoke();
				}
			}
		}

		/// <summary>
		/// Subscribes to a given event
		/// </summary>
		/// <param name="key">The event to subscribe to</param>
		/// <param name="callback">Callback when event is invoked</param>
		public static void On(object key, Action<object[]> callback) {
			if (!m_parameterActions.ContainsKey(key)) {
				m_parameterActions.Add(key, new List<Action<object[]>>());
			}

			m_parameterActions[key].Add(callback);
		}

		/// <summary>
		/// Subscribes to a given event
		/// </summary>
		/// <param name="key">The event to subscribe to</param>
		/// <param name="callback">Callback when event is invoked</param>
		public static void On(object key, Action callback) {
			if (!m_actions.ContainsKey(key)) {
				m_actions.Add(key, new List<Action>());
			}

			m_actions[key].Add(callback);
		}


		/// <summary>
		/// Gets amount of listeners on a given event
		/// </summary>
		/// <param name="key">The event</param>
		public static int ListnerCount(object key) {
			var paramCount = m_parameterActions.ContainsKey(key) ? m_parameterActions[key].Count : 0;
			var normalCount = m_actions.ContainsKey(key) ? m_actions[key].Count : 0;
			return paramCount + normalCount;
		}

		/// <summary>
		/// Removes a listener from a event. This will most likely not work with anonymous functions
		/// </summary>
		/// <param name="key">The event</param>
		/// <param name="action">The action to remove</param>
		public static bool RemoveListner(object key, Action<object[]> action) {
			var success = false;
			if (m_parameterActions.ContainsKey(key)) {
				success = m_parameterActions[key].Remove(action);
				if (m_parameterActions[key].Count == 0) {
					m_parameterActions.Remove(key);
				}
			}

			return success;
		}

		/// <summary>
		/// Removes a listener from a event. This will most likely not work with anonymous functions
		/// </summary>
		/// <param name="key">The event</param>
		/// <param name="action">The action to remove</param>
		public static bool RemoveListner(object key, Action action) {
			var success = false;
			if (m_actions.ContainsKey(key)) {
				success = m_actions[key].Remove(action);
				if (m_actions[key].Count == 0) {
					m_actions.Remove(key);
				}
			}

			return success;
		}

		/// <summary>
		/// Performs a full reset on the event hub, removing all subscribers and event handlers
		/// </summary>
		public static void ResetAll() {
			m_parameterActions.Clear();
			m_actions.Clear();
		}
	}
}