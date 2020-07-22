using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Windows;

namespace DXToolKit.Engine {
	public abstract class DXApp : FunctionToolBox, IDisposable {
		public static DXApp Current;


		protected RenderForm m_renderform => Graphics.Renderform;
		protected GraphicsDevice m_device => Graphics.Device;
		public Color ClearColor = Color.Black;

		private float m_minimumFrameAccumulator;
		private float m_minimumFrameRate = 100.0F;
		private double m_lastUpdateTime;
		private Stopwatch m_updateTimer;
		private bool m_useDynamicUpdates = true;

		private float m_fixedFrameRate = 100.0F;
		private float m_fixedUpdateTimer;
		private string[] m_cmdArgs;

		private IRenderPipeline m_renderPipeline;

		public IRenderPipeline RenderPipeline => m_renderPipeline;

		/// <summary>
		/// Minimum frame rate used for dynamic updates
		/// Default: 100
		/// </summary>
		public float MinimumFrameRate {
			get => m_minimumFrameRate;
			set => m_minimumFrameRate = value;
		}

		/// <summary>
		/// If game should use dynamic updates, meaning that if the frame time is higher then a given amount, multiple updates will be called each frame
		/// </summary>
		public bool UseDynamicUpdates {
			get => m_useDynamicUpdates;
			set => m_useDynamicUpdates = value;
		}

		/// <summary>
		/// Number of fixed update calls every second
		/// </summary>
		public float FixedFrameRate {
			get => m_fixedFrameRate;
			set => m_fixedFrameRate = value;
		}

		public int Run(string[] args) {
			bool logExceptions = false;
			foreach (var arg in args) {
				if (arg.Contains("hidden")) {
					logExceptions = true;
				}
			}

			var runAction = new Action(() => {
				m_cmdArgs = args;
				Current = this;
				Graphics.Setup(m_cmdArgs);
				Graphics.Device.EarlyResizeBegin += (mode, fullscreen) => {
					EngineConfig.SetConfig(mode, new Rational(165, 1), fullscreen, false);
				};
				Input.Initialize(Graphics.Renderform);
				Time.Initialize();
				Debug.Initialize(Graphics.Device);
				m_renderPipeline = CreateRenderPipeline();
				Initialize();
				m_updateTimer = Stopwatch.StartNew();
				RenderLoop.Run(Graphics.Renderform, Frame);
			});

			if (logExceptions) {
				try {
					runAction();
				}
				catch (Exception e) {
					File.WriteAllText("exception.log", e.ToString());
					Exit();
					return 1;
				}
			} else {
				runAction();
			}

			return 0;
		}


		private void Frame() {
			// Get last frame time, and slow down frame rate to target
			Time.Frame();

			m_fixedUpdateTimer += Time.DeltaTime;
			var tempDeltaTime = Time.DeltaTime;
			while (m_fixedUpdateTimer > 1.0F / m_fixedFrameRate) {
				Time.DeltaTime = 1.0F / m_fixedFrameRate;
				FixedUpdate();
				SceneManager.RunFixedUpdate();
				m_fixedUpdateTimer -= 1.0F / m_fixedFrameRate;
			}

			Time.DeltaTime = tempDeltaTime;

			if (m_useDynamicUpdates) {
				// Function for handling if Update is slower then Fixed Frame Rate
				// Basically checks the last frames update time, and if its greater then target, it lowers fixed frame rate by dividing that frame rate by an amount so that the last frame would be completed within the time
				// Base frame rate divider is 1
				var minimumFrameRateDivider = 1;
				// Get fixed frame time target by dividing 1 by the fixed frame rate
				var minimumFrameTimeTarget = 1.0F / (m_minimumFrameRate / minimumFrameRateDivider);
				// While last update is greater then the fixed target
				while (m_lastUpdateTime > minimumFrameTimeTarget) {
					// Double divider (1 - 2 - 4 - 8 - 16 - etc)
					minimumFrameRateDivider *= 2;
					// Set new fixed target with division
					minimumFrameTimeTarget = 1.0F / (m_minimumFrameRate / minimumFrameRateDivider);
					// Set running slowly variables in time object
					Time.IsRunningVerySlowly = true;
					Time.IsRunningSlowly = true;
				}

				// Handler for when Rendering takes longer then fixed frame rate
				// Check if frame time is greater then the minimum fixed amount we want
				if (Time.DeltaTime > minimumFrameTimeTarget) {
					// If it is, add the extra time above fixed target
					m_minimumFrameAccumulator += Time.DeltaTime - minimumFrameTimeTarget;
					// Set running slowly to allow for user to not execute heavy code
					Time.IsRunningSlowly = true;
				}

				// Set update count to 1
				int updateCount = 1;
				// Subtract from accumulator while its above the fixed target
				while (m_minimumFrameAccumulator > minimumFrameTimeTarget) {
					m_minimumFrameAccumulator -= minimumFrameTimeTarget;
					// Add another update for this frame
					updateCount += 1;
				}

				// Update delta time to correctly reflect the new value
				if (updateCount > 1) {
					Time.DeltaTime /= updateCount;
				}

				// Start timer to get the actual time update takes
				m_updateTimer.Restart();
				for (int i = 0; i < updateCount; i++) {
					// Run normal application update
					Input.Frame(Graphics.Renderform);
					Update();
					SceneManager.RunUpdate();

					// Handler to make sure debug is only written once per frame, this does not account for timed updates
					if (updateCount > 1 && i < updateCount - 1) {
						// TODO - make a nifty handler for this so that timed logs actually print correctly (At the moment they will print updateCount number of times, while they should only print once every frame)
						// TODO - Can probably be fixed if debug renders to a separate target then the back buffer, and when the back buffer is presented, it just renders that texture to the screen
						Debug.ClearFrameLog();
						if (Time.ShowFPS) {
							Debug.Log("FPS " + Time.FPS.ToString("0.00"));
						}
					}
				}

				// Stop timer that checks the actual execution time of update
				m_updateTimer.Stop();
				// Set variable to use for next frame, so if its greater then fixed time we can lower the fixed frame rate
				m_lastUpdateTime = m_updateTimer.ElapsedTicks / (double) Stopwatch.Frequency;
			} else {
				// Run normal application update
				Input.Frame(Graphics.Renderform);
				Update();
				SceneManager.RunUpdate();
			}

			// Run draw as much as possible
			m_renderPipeline.Begin(ClearColor);
			Debug.Run3DRender(m_device);
			Render();
			SceneManager.RunRender();
			Debug.Render(Graphics.Device);
			m_renderPipeline.Present(EngineConfig.UseVsync ? 1 : 0);
		}

		public void Exit() {
			Graphics.Renderform.Close();
		}

		public void Dispose() {
			Time.Shutdown();
			SceneManager.Shutdown();
			OnDispose();

			m_renderPipeline?.Dispose();
			Input.Shutdown();
			Debug.Shutdown();
			Graphics.Shutdown();
		}

		protected virtual void OnDispose() { }
		protected virtual void Initialize() { }
		protected virtual void Update() { }
		protected virtual void FixedUpdate() { }
		protected virtual void Render() { }

		protected virtual IRenderPipeline CreateRenderPipeline() {
			return new BasicPipeline(m_device);
		}
	}
}