using System;
using System.Collections.Generic;

namespace DXToolKit.Engine {
	public static class SceneManager {
		private static Dictionary<object, Scene> m_scenes = new Dictionary<object, Scene>();
		private static Scene m_activeScene;

		public static T AddScene<T>(T scene, object key, bool load = false) where T : Scene {
			if (scene == null) throw new ArgumentNullException(nameof(scene));
			if (key == null) throw new ArgumentNullException(nameof(key));
			m_scenes.Add(key, scene);

			if (load) {
				m_activeScene?.RunUnload();
				m_activeScene = scene;
				m_activeScene.RunLoad();
			}

			return scene;
		}

		public static T LoadScene<T>(object key) where T : Scene {
			if (key == null) throw new ArgumentNullException(nameof(key));
			m_activeScene?.RunUnload();
			m_activeScene = m_scenes[key];
			m_activeScene.RunLoad();
			return (T) m_scenes[key];
		}

		public static Scene LoadScene(object key) {
			if (key == null) throw new ArgumentNullException(nameof(key));
			m_activeScene?.RunUnload();
			m_activeScene = m_scenes[key];
			m_activeScene.RunLoad();
			return m_scenes[key];
		}

		internal static void RunUpdate() {
			m_activeScene?.RunUpdate();
		}

		internal static void RunRender() {
			m_activeScene?.RunRender();
		}

		internal static void RunFixedUpdate() {
			m_activeScene?.RunFixedUpdate();
		}


		internal static void Shutdown() {
			foreach (var scene in m_scenes) {
				scene.Value?.Dispose();
			}

			m_activeScene?.Dispose();
		}
	}
}