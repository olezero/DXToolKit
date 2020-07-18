using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DXToolKit.Engine {
	public static class Profiler {
		private class ProfilingJob {
			private bool m_isRunning;
			private Stopwatch m_stopwatch;
			private long[] m_times;

			private int m_index;
			private bool m_firstRun = true;

			public ProfilingJob() {
				m_isRunning = false;
				m_stopwatch = new Stopwatch();
				m_times = new long[16];
			}

			public void Start() {
				if (m_isRunning) {
					throw new Exception("Cannot start a profiler while its already running");
				}

				m_stopwatch.Reset();
				m_stopwatch.Start();
				m_isRunning = true;
			}

			public void End() {
				if (m_isRunning == false) {
					throw new Exception("Cannot stop a profiler without starting it");
				}

				m_stopwatch.Stop();
				m_isRunning = false;

				m_times[m_index] = m_stopwatch.ElapsedTicks;
				m_index++;
				if (m_index > m_times.Length - 1) {
					m_index = 0;
					m_firstRun = false;
				}
			}


			public double GetAverageTime() {
				double averageTime = 0;

				if (m_firstRun) {
					// Add m_times[0 - m_index] / m_index
					for (int i = 0; i < m_index; i++) {
						averageTime += m_times[i];
					}

					averageTime /= m_index;
				} else {
					// Add all m_times / count
					for (int i = 0; i < m_times.Length; i++) {
						averageTime += m_times[i];
					}

					averageTime /= m_times.Length;
				}

				return averageTime / Stopwatch.Frequency * 1000;
			}
		}

		private static Dictionary<object, ProfilingJob> m_profilers = new Dictionary<object, ProfilingJob>();

		public static void Profile(string name, Action action, bool print = true) {
			StartProfiler(name);
			action.Invoke();
			StopProfiler(name);
			if (print) {
				DebugLog(name);
			}
		}

		public static void StartProfiler(object index) {
			if (m_profilers.ContainsKey(index)) {
				m_profilers[index].Start();
			} else {
				m_profilers.Add(index, new ProfilingJob());
				m_profilers[index].Start();
			}
		}

		public static void StopProfiler(object index, bool debugLog = false, float displayTime = -1F) {
			if (m_profilers.ContainsKey(index)) {
				m_profilers[index].End();
			}

			if (debugLog) {
				DebugLog(index, displayTime);
			}
		}

		public static double GetMetrics(object index) {
			return m_profilers[index].GetAverageTime();
		}

		public static void DebugLog(object index, float displayTime = -1) {
			if (m_profilers.ContainsKey(index)) {
				Debug.Log(index + " " + m_profilers[index].GetAverageTime().ToString("0.000ms"),
					displayTime);
			}
		}
	}
}