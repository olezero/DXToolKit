using System.Runtime.InteropServices;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using SharpDX.DXGI;

namespace DXToolKit.Sandbox {
	public class RayTracingSketch : Sketch {
		[StructLayout(LayoutKind.Sequential)]
		struct CBufferType {
			public Matrix CameraToWorldMatrix;
			public Matrix InverseProjectionMatrix;
		}

		private ComputeShader m_computeShader;
		private ShaderCompilationResult m_compilationResult;
		private Texture2D m_renderTexture;
		private ShaderResourceView m_srv;
		private RenderTargetView m_rtv;
		private UnorderedAccessView m_uav;
		private SpriteBatch m_spriteBatch;
		private ConstantBuffer<CBufferType> m_cbuffer;
		private Camera3D m_camera3D;

		protected override void OnLoad() {
			TextureLoader.SRVFromFile(m_device, @"C:\Users\Ole\Downloads\cape_hill_1k.hdr");
			
			m_renderTexture = new Texture2D(m_device, new Texture2DDescription {
				Format = Format.R32G32B32A32_Float,
				Width = EngineConfig.ScreenWidth,
				Height = EngineConfig.ScreenHeight,
				Usage = ResourceUsage.Default,
				ArraySize = 1,
				BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess | BindFlags.RenderTarget,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(1, 0),
				CpuAccessFlags = CpuAccessFlags.None,
			});
			m_srv = new ShaderResourceView(m_device, m_renderTexture);
			m_uav = new UnorderedAccessView(m_device, m_renderTexture);
			m_rtv = new RenderTargetView(m_device, m_renderTexture);
			m_spriteBatch = new SpriteBatch(m_device);
			m_cbuffer = new ConstantBuffer<CBufferType>(m_device);


			LiveReload.CreateWatcher(@"C:\HLSL_Shaders\raytrace.hlsl", file => {
				m_computeShader?.Dispose();
				m_computeShader = ShaderCompiler.TryCompile<ComputeShader>(m_device, file, "CSMain", out m_compilationResult);
				Debug.Log("Compiled!", 1000);
			}, true);

			m_camera3D = new Camera3D();
			Debug.SetD3DCamera(m_camera3D);
			m_camera3D.LoadFromFile("camerapos");
		}

		protected override void Update() {
			m_camera3D.Update();


			if (!m_compilationResult.Success) {
				Debug.Log(m_compilationResult.Message);
			}
		}

		protected override void Render() {
			Debug.Cube(Matrix.Identity, Color.White);

			if (m_compilationResult.Success) {
				m_cbuffer.Write(new CBufferType {
					InverseProjectionMatrix = Matrix.Invert(m_camera3D.ProjectionMatrix),
					CameraToWorldMatrix = Matrix.Invert(m_camera3D.ViewMatrix)
				});

				m_context.ComputeShader.Set(m_computeShader);
				m_context.ComputeShader.SetUnorderedAccessView(0, m_uav);
				m_context.ComputeShader.SetConstantBuffer(0, m_cbuffer);
				
				m_context.Dispatch(Mathf.Ceiling(EngineConfig.ScreenWidth / 8.0F), Mathf.Ceiling(EngineConfig.ScreenHeight / 8.0F), 1);
				m_context.ComputeShader.SetConstantBuffer(0, null);
				m_context.ComputeShader.SetUnorderedAccessView(0, null);
				m_context.ComputeShader.Set(null);

				Debug.Log("Drawing");

				
				// m_context.ClearRenderTargetView(m_rtv, Color.Red * 1.0F);
				m_spriteBatch.Draw(m_srv, new RectangleF(0, 0, EngineConfig.ScreenWidth, EngineConfig.ScreenHeight));
			}

			m_spriteBatch.Render();
		}

		protected override void OnUnload() {
			m_camera3D.SaveToFile("camerapos");

			Utilities.Dispose(ref m_computeShader);
			Utilities.Dispose(ref m_renderTexture);
			Utilities.Dispose(ref m_srv);
			Utilities.Dispose(ref m_rtv);
			Utilities.Dispose(ref m_uav);
			Utilities.Dispose(ref m_spriteBatch);
			Utilities.Dispose(ref m_cbuffer);
		}
	}
}