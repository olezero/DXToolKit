using System;
using System.Collections.Generic;

namespace DXToolKit.Engine {
	public static class EventHub {
		private static Dictionary<object, List<Action>> m_actions = new Dictionary<object, List<Action>>();
		private static Dictionary<object, List<Action<object[]>>> m_parameterActions = new Dictionary<object, List<Action<object[]>>>();

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


		public static void On(object key, Action<object[]> callback) {
			if (!m_parameterActions.ContainsKey(key)) {
				m_parameterActions.Add(key, new List<Action<object[]>>());
			}

			m_parameterActions[key].Add(callback);
		}

		public static void On(object key, Action callback) {
			if (!m_actions.ContainsKey(key)) {
				m_actions.Add(key, new List<Action>());
			}

			m_actions[key].Add(callback);
		}


		public static int ListnerCount(object key) {
			var paramCount = m_parameterActions.ContainsKey(key) ? m_parameterActions[key].Count : 0;
			var normalCount = m_actions.ContainsKey(key) ? m_actions[key].Count : 0;
			return paramCount + normalCount;
		}

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