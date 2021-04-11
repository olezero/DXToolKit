using System;
using DXToolKit.Engine;
using SharpDX.Direct2D1;
using SharpDX.Mathematics;
using SharpDX;
using SharpDX.DXGI;

namespace DXToolKit.Sandbox {
	public class FrameRateTest : Sketch {
		private SolidColorBrush m_brush;
		private float m_30Timer;
		private float m_60Timer;
		private float m_120Timer;

		private float m_30Offset;
		private float m_60Offset;
		private float m_120Offset;

		private float m_liveOffset;
		private float m_timer;
		private float m_moveSpeed = 1.0F;

		private Camera3D m_camera;


		protected override void OnLoad() {
			EnableGUI();
			GUI.Append(new Slider(GUIDirection.Horizontal, f => {
				m_moveSpeed = (float) Math.Log(f, 10);
			}) {
				MinValue = 0.001F,
				MaxValue = 10.0F,
				Value = 1.0F,
				Width = 400,
				X = 100,
				Y = 100,
			});

			m_brush = new SolidColorBrush(m_device, Color.White);
			Time.TargetFrameRate = 200;


			/*
			Graphics.Device.Resize(new ModeDescription {
				Format = Format.R8G8B8A8_UNorm,
				Height = 1440,
				Width = 2560,
				RefreshRate = new Rational(165000, 1000)
			}, true);
			*/
			EngineConfig.UseVsync = false;

			DXApp.Current.FixedFrameRate = 60;
			DXApp.Current.MinimumFrameRate = 60;
			DXApp.Current.UseDynamicUpdates = false;

			m_camera = new Camera3D();
			Debug.SetD3DCamera(m_camera);
			m_camera.ToggleOrbitCamera(Vector3.Zero);
		}

		protected override void Update() {
			if (Time.DeltaTime > 1.0F / 195.0) {
				Debug.Log(Time.DeltaTime, 1000);
			}

			m_30Timer += Time.DeltaTime;
			m_60Timer += Time.DeltaTime;
			m_120Timer += Time.DeltaTime;

			if (m_30Timer > 1.0F / 30.0F) {
				m_30Offset = m_liveOffset;
				m_30Timer = 0;
			}

			if (m_60Timer > 1.0F / 60.0F) {
				m_60Offset = m_liveOffset;
				m_60Timer = 0;
			}

			if (m_120Timer > 1.0F / 200.0F) {
				m_120Offset = m_liveOffset;
				m_120Timer = 0;
			}

			m_timer += Time.DeltaTime * m_moveSpeed;
			m_liveOffset = (Mathf.Sin(m_timer) + 1) / 2.0f * (EngineConfig.ScreenWidth - 100);
			m_camera.Update();
		}

		protected override void Render() {
			m_renderTarget.BeginDraw();

			var yStep = EngineConfig.ScreenHeight / 4.0F;
			var yOffset = -50;


			// m_renderTarget.FillRectangle(new RectangleF(m_liveOffset, yOffset + yStep * 0, 100, 100), m_brush);

			m_renderTarget.FillRectangle(new RectangleF(m_30Offset, yOffset + yStep * 1, 100, 100), m_brush);
			m_renderTarget.FillRectangle(new RectangleF(m_60Offset, yOffset + yStep * 2, 100, 100), m_brush);
			m_renderTarget.FillRectangle(new RectangleF(m_120Offset, yOffset + yStep * 3, 100, 100), m_brush);


			m_renderTarget.EndDraw();


			Debug.Cube(Matrix.Identity, Color.White);
		}

		protected override void OnUnload() { }
	}
}