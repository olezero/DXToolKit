using DXToolKit.Engine;
using SharpDX;
using SharpDX.DirectInput;

namespace DXToolKit.Sandbox {
	public class TransformSketch : Sketch {
		private Transform m_transform;
		private Transform m_parentTransform;
		private Camera3D m_camera3D;

		private Transform[] m_transforms;

		protected override void OnLoad() {
			m_transform = new Transform();
			m_parentTransform = new Transform();
			m_transform.Parent = m_parentTransform;
			m_transform.Translate(0, 2, 0);
			m_parentTransform.Translate(1, 0, 0);
			// m_transform.Scale(0.5F);


			m_camera3D = new Camera3D();
			m_camera3D.LoadFromFile("camera");
			m_camera3D.ToggleOrbitCamera(Vector3.Zero);
			Debug.SetD3DCamera(m_camera3D);


			/*
			int count = 3;
			Transform lastTransform = null;
			m_transforms = new Transform[count * count * count];
			int counter = 0;
			for (int y = 0; y < count; y++) {
				for (int x = 0; x < count; x++) {
					for (int z = 0; z < count; z++) {
						var newTransform = new Transform() {
							Position = new Vector3(x, y, z),
						};

						newTransform.Parent = lastTransform;

						m_transforms[counter++] = newTransform;

						lastTransform = newTransform;
					}
				}
			}
			*/
		}

		protected override void Update() {
			m_camera3D.Update();

			if (Input.KeyDown(Key.F1)) {
				if (m_transform.Parent != null) {
					m_transform.Parent = null;
				} else {
					m_transform.Parent = m_parentTransform;
				}
			}

			Debug.Log("HasParent: " + (m_transform.Parent != null));
			
			


			// m_transform.Rotate(Mathf.Pi * Time.DeltaTime, 0, 0);
			// m_transform.Translate(m_transform.Forward * Time.DeltaTime);
			// m_parentTransform.Rotate(0, Mathf.Pi * Time.DeltaTime, 0);
		}

		protected override void Render() {
			Debug.Cube(m_transform.World, Color.CornflowerBlue);
			Debug.Cube(m_parentTransform.World, Color.PaleVioletRed);

			// foreach (var transform in m_transforms) {
			// Debug.Cube(transform.World, Color.White);
			// }
		}

		protected override void OnUnload() {
			m_camera3D.SaveToFile("camera");
		}
	}
}