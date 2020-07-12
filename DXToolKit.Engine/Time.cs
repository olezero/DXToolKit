using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace DXToolKit {
	public static class Time {
		[DllImport("winmm.dll")]
		internal static extern uint timeBeginPeriod(uint period);

		[DllImport("winmm.dll")]
		internal static extern uint timeEndPeriod(uint period);

		private class FrameCounter {
			private readonly double[] m_frameTimes = new double[25];
			private int m_currentFrame;
			public double AverageFPS => 1.0 / (m_frameTimes.Sum() / m_frameTimes.Length);
			private bool m_first = true;

			public void Frame(double frameTime) {
				if (m_first) {
					for (var i = 0; i < m_frameTimes.Length; i++) {
						m_frameTimes[i] = frameTime;
					}

					m_first = false;
				}

				m_frameTimes[m_currentFrame++] = frameTime;
				if (m_currentFrame >= m_frameTimes.Length) {
					m_currentFrame = 0;
				}
			}
		}

		private static bool m_skipfirst = true;
		private static Stopwatch m_frameTimer;
		private static Stopwatch m_appTimer;
		private static FrameCounter m_frameCounter;

		private static long m_frameTimeTicks;
		private static long m_targetFrameTimeTicks;

		private static double m_frameTime;

		public static float DeltaTime {
			get => (float) m_frameTime;
			internal set => m_frameTime = value;
		}

		public static float AppTime => m_appTimer.ElapsedMilliseconds / 1000.0F;
		public static float FPS => (float) m_frameCounter.AverageFPS;
		public static bool ShowFPS;

		public static bool IsRunningSlowly { get; internal set; }
		public static bool IsRunningVerySlowly { get; internal set; }

		public static int TargetFrameRate {
			set {
				if (value <= 0) {
					m_targetFrameTimeTicks = -1;
					return;
				}

				m_targetFrameTimeTicks = Stopwatch.Frequency / value;
			}
		}

		internal static void Pause() {
			m_frameTimer.Stop();
			m_appTimer.Stop();
		}

		internal static void Resume() {
			m_frameTimer.Start();
			m_appTimer.Start();
		}

		internal static void Initialize() {
			m_frameTimer = Stopwatch.StartNew();
			m_appTimer = Stopwatch.StartNew();
			m_frameCounter = new FrameCounter();
			TargetFrameRate = 200;
			timeBeginPeriod(1);
		}

		internal static void Shutdown() {
			timeEndPeriod(1);
		}

		internal static void Frame() {
			if (m_skipfirst) {
				m_skipfirst = false;
				return;
			}

			// End previous period
			timeEndPeriod(1);
			
			// Start next period (the frame)
			timeBeginPeriod(1);

			IsRunningSlowly = false;
			IsRunningVerySlowly = false;

			m_frameTimeTicks = m_frameTimer.ElapsedTicks;

			while (m_frameTimeTicks < m_targetFrameTimeTicks) {
				var difference = (int) Math.Floor((double) ((m_targetFrameTimeTicks - m_frameTimeTicks) * 1000) /
				                                  Stopwatch.Frequency);
				if (difference > 1) {
					System.Threading.Thread.Sleep(difference - 1);
				}

				m_frameTimeTicks = m_frameTimer.ElapsedTicks;
			}

			m_frameTimer.Restart();
			m_frameTime = (double) m_frameTimeTicks / Stopwatch.Frequency;
			m_frameCounter.Frame(m_frameTime);

			if (ShowFPS) {
				Debug.Log(FPS.ToString("FPS 0.00"));
			}


			for (int i = 0; i < m_actions.Count; i++) {
				m_actions[i].TimeToCall -= DeltaTime * 1000;
				if (m_actions[i].TimeToCall < 0) {
					m_actions[i].Action?.Invoke();
					m_actions.RemoveAt(i--);
				}
			}
		}


		public class DelayedAction {
			public float TimeToCall;
			public Action Action;
		}

		private static List<DelayedAction> m_actions = new List<DelayedAction>();

		public static DelayedAction SetTimeout(float ms, Action action) {
			var result = new DelayedAction() {
				TimeToCall = ms,
				Action = action,
			};
			m_actions.Add(result);
			return result;
		}

		public static void ClearTimeout(ref DelayedAction action) {
			if (action != null) {
				m_actions.Remove(action);
			}
		}
	}
}