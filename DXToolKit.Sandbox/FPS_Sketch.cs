using System;
using System.Windows.Forms;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectInput;

namespace DXToolKit.Sandbox {
	public class FPS_Sketch : Sketch {
		// TODO: Separate acceleration/velocity for strafe engines (x/y movement), main engines (z movement) and rcs (yaw, pitch and roll) 
		// TODO: Clamp values within these to ship metrics (rpm for RCS, units/s for strafe and z forward/backward)
		// Should set values for max speed and acceleration in ((units per second) per second) so it becomes more readable

		// Yaw/Pitch should always have the same acceleration
		// Roll could be less since its a more "volatile" move
		// Forward acceleration can be modified by "boost"
		// Strafe acceleration in all directions should be the same
		// Reverse acceleration should be lower then forward acceleration (probably the same as strafe acceleration)


		/// <summary>
		/// Convert control input to power
		/// </summary>
		private Vector3 m_strafePower;

		private Vector3 m_rcsPower;
		private float m_enginePower;


		private const float FORWARD_POWER = 1.0F;
		private const float BACKWARD_POWER = 0.2F;
		private const float STRAFE_POWER = 0.2F;
		private const float YAW_POWER = 0.2F;
		private const float PITCH_POWER = 0.2F;
		private const float ROLL_POWER = 0.1F;


		// Clamp velocity like this ? Dont use "drag". This way when strafing forward velocity will be lost to compensate, while still keeping the ship at "max speed"
		// Have to check if this feels ok
		// Normalize velocity to MAX_SPEED. 
		// TODO If length (velocity) > MAX_SPEED => velocity *= 1 - length(velocity) - MAX_SPEED
		// Alternate if len(vel) > MAX_SPEED => velocity = normalize(vel) * MAX_SPEED


		// 0. Reset all acceleration variables ? 
		// 1. Retrieve input and set engine powers accordingly (might do a bit of a lerp to simulate engine lag from input to full power, but check if it feels ok) This is local based
		// 2. Add up acceleration based on inputs (Rotate input to match ship rotation) so that acceleration is a "world" based direction
		// 3. Add acceleration to velocity (world based)
		// 4. Clamp velocity to MAX_SPEED (normalize * MAX_SPEED)
		// 5. Add velocity to position

		// TODO: Might need strafe AND forward velocity with independent MAX_SPEED variables for each, where MAX_SPEED for forward should be the current "engine setting" set by the user. And MAX engine setting is just directly limited to ship config
		// Strafe velocity should be clamped to ship config
		// Final velocity is just forward + strafe velocity
		// TODO: Figure out breaking force when speeding down / stopping strafe
		// If velocity > target velocity => velocity /= 1 + BREAKING_FORCE ?

		// TODO: See if there is a possibility for a custom inertia dampener that tries to move the current ship forward vector towards the target vector
		// TODO: Include mass in this calculation?


		private SolidColorBrush m_brush;
		private DXCamera m_camera;
		private float m_movespeed = 0.0F;
		private StrokeStyle m_dashStyle;
		private Vector3 m_momentum;
		private Vector3 m_straftPower;
		private float m_yawForce = 0.0F;
		private float m_pitchForce = 0.0F;
		private float m_rollForce = 0.0F;
		private Matrix m_rotationMatrix;


		protected override void OnLoad() {
			// Time.TargetFrameRate = 60;
			m_brush = new SolidColorBrush(m_device, Color.White);
			m_camera = new DXCamera();
			Debug.SetD3DCamera(m_camera);
			m_camera.FarClippingPlane = 10000;
			m_camera.FieldOfView = 90.0F;

			m_device.OnResizeEnd += () => {
				m_camera.AspectRatio = (float) EngineConfig.ScreenWidth / (float) EngineConfig.ScreenHeight;
				Debug.Log(m_camera.AspectRatio, 10000);
			};

			m_camera.AspectRatio = (float) EngineConfig.ScreenWidth / (float) EngineConfig.ScreenHeight;
			Debug.Log(m_camera.AspectRatio, 10000);

			// m_camera.AspectRatio = 16.0F / 9.0F;
			// m_camera.ClampPitch = false;


			m_dashStyle = new StrokeStyle(m_device.Factory, new StrokeStyleProperties() {
				DashStyle = DashStyle.DashDotDot,
				DashOffset = 0,
			});
			m_rotationMatrix = Matrix.Identity;

			// Cursor.Hide();
		}

		protected override void Update() { }

		protected override void Render() {
			var rnd = new Random(0);

			for (int i = 0; i < 1000; i++) {
				var location = rnd.NextVector3(Vector3.One * -1000, Vector3.One * 1000);
				Debug.Cube(Matrix.Scaling(10) * Matrix.Translation(location), Color.White);
			}


			var rt = m_renderTarget;

			// rt.Transform = Matrix3x2.Translation(EngineConfig.ScreenWidth / 2.0F, EngineConfig.ScreenHeight / 2.0F);
			rt.BeginDraw();
			// rt.DrawEllipse(new Ellipse(new Vector2(0, 0), 20, 20), m_brush, 2);

			const float MAX_DIST = 300.0F;
			const float DEADZONE = 0.0F;
			const float RUDDER_MULTI = 2.000F;

			var centerScreen = new Vector2(EngineConfig.ScreenWidth / 2.0F, EngineConfig.ScreenHeight / 2.0F);
			var renderPosition = MousePosition;
			var len = Vector2.Distance(centerScreen, renderPosition);
			var dir = Vector2.Normalize(renderPosition - centerScreen);

			if (len > MAX_DIST) {
				renderPosition = centerScreen + dir * MAX_DIST;
				len = MAX_DIST;
			}

			if (len < DEADZONE) {
				len = 0;
			}


			float pitchAmount = centerScreen.Y - renderPosition.Y;
			float yawAmount = centerScreen.X - renderPosition.X;
			float rollAmount = 0.0F;

			if (Mathf.Abs(pitchAmount) < DEADZONE) pitchAmount = 0;
			if (Mathf.Abs(yawAmount) < DEADZONE) yawAmount = 0;

			pitchAmount /= MAX_DIST;
			yawAmount /= MAX_DIST;

			pitchAmount *= RUDDER_MULTI * Time.DeltaTime;
			yawAmount *= RUDDER_MULTI * Time.DeltaTime;


			if (Input.KeyPressed(Key.Q)) rollAmount = 1 * RUDDER_MULTI * Time.DeltaTime;
			if (Input.KeyPressed(Key.E)) rollAmount = -1 * RUDDER_MULTI * Time.DeltaTime;


			DrawCrossHair(centerScreen, renderPosition);
			rt.FillEllipse(new Ellipse(centerScreen, 2, 2), m_brush);


			m_yawForce = Mathf.Lerp(m_yawForce, yawAmount, Time.DeltaTime * 8);
			m_pitchForce = Mathf.Lerp(m_pitchForce, pitchAmount, Time.DeltaTime * 8);
			m_rollForce = Mathf.Lerp(m_rollForce, rollAmount, Time.DeltaTime * 8);


			m_rotationMatrix *= Matrix.RotationAxis(m_rotationMatrix.Up, -m_yawForce);
			m_rotationMatrix *= Matrix.RotationAxis(m_rotationMatrix.Right, -m_pitchForce);
			m_rotationMatrix *= Matrix.RotationAxis(m_rotationMatrix.Forward, -m_rollForce);


			m_camera.RotationMatrix = m_rotationMatrix;


			if (Input.KeyPressed(Key.LeftShift)) {
				m_movespeed += 1.0F * Time.DeltaTime;
			}

			if (Input.KeyPressed(Key.LeftControl)) {
				m_movespeed -= 1.0F * Time.DeltaTime;
			}


			if (Input.KeyPressed(Key.W)) m_momentum += m_rotationMatrix.Up * Time.DeltaTime;
			if (Input.KeyPressed(Key.A)) m_momentum += m_rotationMatrix.Left * Time.DeltaTime;
			if (Input.KeyPressed(Key.S)) m_momentum += m_rotationMatrix.Down * Time.DeltaTime;
			if (Input.KeyPressed(Key.D)) m_momentum += m_rotationMatrix.Right * Time.DeltaTime;


			m_momentum += m_camera.ForwardVector * m_movespeed * Time.DeltaTime;

			Debug.Log(m_movespeed);

			// m_camera.Translate(0, 0, m_movespeed);
			m_camera.Position += m_momentum;


			if (m_momentum.Length() > 1) {
				m_momentum.Normalize();
			}

			// m_momentum *= 0.99F;

			rt.EndDraw();
		}

		protected override void OnUnload() {
			Utilities.Dispose(ref m_dashStyle);
			Utilities.Dispose(ref m_brush);
		}


		private void DrawCrossHair(Vector2 centerScreen, Vector2 position) {
			const float LINE_WIDTH = 2.0F;
			const float size = 10.0F;
			var rt = m_renderTarget;
			rt.DrawEllipse(new Ellipse(position, size, size), m_brush, LINE_WIDTH);
			rt.DrawLine(position - Vector2.UnitY * size * 1.5F, position + Vector2.UnitY * size * 1.5F, m_brush, LINE_WIDTH);
			rt.DrawLine(position - Vector2.UnitX * size * 1.5F, position + Vector2.UnitX * size * 1.5F, m_brush, LINE_WIDTH);


			rt.DrawLine(centerScreen, position, m_brush, LINE_WIDTH, m_dashStyle);
		}
	}
}