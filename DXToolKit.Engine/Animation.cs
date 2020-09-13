using System;
using System.Collections.Generic;

namespace DXToolKit.Engine {
	/// <summary>
	/// Animation handler for the application.
	/// Runs once each update and invoking all event handlers until they are done
	/// </summary>
	public static class Animation {
		/// <summary>
		/// Animation event handler
		/// </summary>
		/// <param name="from">From value</param>
		/// <param name="to">To value</param>
		/// <param name="amount">The current amount</param>
		public delegate bool AnimationEventHandler(float from, float to, float amount);

		private class AnimationData {
			private AnimationEventHandler m_eventHandler;
			private Action m_onComplete;
			private float m_runTime = 0.0F;
			private float m_fromVale = 0.0F;
			private float m_toValue = 0.0F;
			private float m_current = 0.0F;

			public AnimationData(float runTime, float from, float to, AnimationEventHandler eventHandler, Action onComplete = null) {
				m_runTime = runTime;
				m_toValue = to;
				m_fromVale = from;
				m_eventHandler = eventHandler;
				m_onComplete = onComplete;
			}

			public void Update(float deltaTime, out bool isDone) {
				m_current += deltaTime * 1000;
				var amount = m_current / m_runTime;
				if (amount > 1.0F) {
					m_eventHandler.Invoke(m_fromVale, m_toValue, 1);
					m_onComplete?.Invoke();
					isDone = true;
				} else {
					// If user returns false, stop animation
					if (m_eventHandler.Invoke(m_fromVale, m_toValue, amount) == false) {
						isDone = true;
						m_onComplete?.Invoke();
						return;
					}

					isDone = false;
				}
			}
		}

		private static List<AnimationData> m_animations = new List<AnimationData>();

		internal static void Update() {
			for (int i = 0; i < m_animations.Count; i++) {
				m_animations[i].Update(Time.DeltaTime, out var isDone);
				if (isDone) {
					m_animations.RemoveAt(i--);
				}
			}
		}

		/// <summary>
		/// Adds an animation to the animation handler
		/// </summary>
		/// <param name="from">The value to go from</param>
		/// <param name="to">The value to go to</param>
		/// <param name="timeInMilliseconds">The time in milliseconds for the animation to execute</param>
		/// <param name="callback">Called once per frame with updated values based on from/to/timeInMilliseconds</param>
		/// <param name="onComplete">Called once the animation has completed</param>
		public static void AddAnimation(float from, float to, float timeInMilliseconds, AnimationEventHandler callback, Action onComplete = null) {
			m_animations.Add(new AnimationData(timeInMilliseconds, from, to, callback, onComplete));
		}


		internal static void Shutdown() {
			m_animations.Clear();
		}
	}
}