using System;
using System.Collections.Generic;
using System.IO;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using DeviceChild = SharpDX.Direct3D11.DeviceChild;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace DXToolKit.Sandbox {

	public class SimpleShaderSketch : Sketch {
		private StructuredBuffer<EntityData> m_structuredBuffer;
		private ComputeShader m_computeShader;
		private ComputeShader m_initShader;
		private ShaderCompilationResult m_compilationResult;
		private FullscreenQuad m_fullscreenQuad;
		private PixelShader m_fullscreenQuadPS;
		private ShaderCompilationResult m_psCompilationResult;
		private ShaderResourceView m_shaderResourceView;
		private Texture2D m_texture2D;
		private UnorderedAccessView m_uav;
		private SamplerState m_samplerState;
		private RenderTargetView m_rtv;
		private bool m_runInit = true;
		private SpriteBatch m_spriteBatch;

		private struct EntityData {
			private uint PositionX;
			private uint PositionY;
			private bool Active;
		}

		protected override void OnLoad() {
			m_structuredBuffer = new StructuredBuffer<EntityData>(m_device, 128 * 128);
			m_fullscreenQuad = new FullscreenQuad(m_device);
			m_texture2D = new Texture2D(m_device, new Texture2DDescription {
				Format = Format.R32G32B32A32_Float,
				Height = 128,
				Width = 128,
				Usage = ResourceUsage.Default,
				ArraySize = 1,
				BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess | BindFlags.RenderTarget,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(1, 0),
				CpuAccessFlags = CpuAccessFlags.None,
			});
			m_shaderResourceView = new ShaderResourceView(m_device, m_texture2D);
			m_uav = new UnorderedAccessView(m_device, m_texture2D);
			m_rtv = new RenderTargetView(m_device, m_texture2D);

			LiveReload.CreateWatcher(@"C:\HLSL_Shaders\simple.hlsl", file => {
				m_computeShader?.Dispose();
				m_computeShader = ShaderCompiler.TryCompile<ComputeShader>(m_device, file, "CSMain", out m_compilationResult);

				if (m_compilationResult.Success) {
					m_initShader?.Dispose();
					m_initShader = ShaderCompiler.TryCompile<ComputeShader>(m_device, file, "Init", out m_compilationResult);
					m_runInit = true;
				}

				Debug.Log("Compiled CS", 1000);
			}, true);

			LiveReload.CreateWatcher(@"C:\HLSL_Shaders\fullscreenQuadPS.hlsl", file => {
				m_fullscreenQuadPS?.Dispose();
				m_fullscreenQuadPS = ShaderCompiler.TryCompile<PixelShader>(m_device, file, "PS", out m_psCompilationResult);
				Debug.Log("Compiled PS", 1000);
			}, true);

			var tmpTex = new Texture2D(m_device, new Texture2DDescription() {
				Format = Format.R32G32B32A32_Float,
				Height = 128,
				Width = 128,
				Usage = ResourceUsage.Dynamic,
				ArraySize = 1,
				BindFlags = BindFlags.ShaderResource,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(1, 0),
				CpuAccessFlags = CpuAccessFlags.Write,
			});

			m_context.MapSubresource(tmpTex, 0, 0, MapMode.WriteDiscard, MapFlags.None, out var stream);
			var colors = new Vector4[128 * 128];
			for (int y = 0; y < 128; y++) {
				for (int x = 0; x < 128; x++) {
					var index = x + y * 128;
					colors[index].X = y / 128.0F;
				}
			}

			stream.WriteRange(colors);
			m_context.UnmapSubresource(tmpTex, 0);
			m_context.CopyResource(tmpTex, m_texture2D);


			m_samplerState = new SamplerState(m_device, new SamplerStateDescription() {
				Filter = Filter.MinMagMipPoint,
				AddressU = TextureAddressMode.Wrap,
				AddressV = TextureAddressMode.Wrap,
				AddressW = TextureAddressMode.Wrap,
			});

			m_spriteBatch = new SpriteBatch(m_device);
		}

		protected override void Update() {
			if (m_compilationResult != null) {
				if (!m_compilationResult.Success) {
					Debug.Log(m_compilationResult.Message);
				}
			}

			if (m_psCompilationResult != null) {
				if (!m_psCompilationResult.Success) {
					Debug.Log(m_psCompilationResult.Message);
				}
			}
		}

		protected override void Render() {
			if (m_compilationResult.Success) {
				m_context.ComputeShader.SetUnorderedAccessView(0, m_structuredBuffer.UAV);
				m_context.ComputeShader.SetUnorderedAccessView(1, m_uav);

				if (m_runInit) {
					Debug.Log("Running init", 1000);
					m_context.ComputeShader.Set(m_initShader);
					m_context.Dispatch(128 / 32, 128 / 32, 1);
					m_runInit = false;
				}

				m_context.ComputeShader.Set(m_computeShader);
				m_context.Dispatch(128 / 32, 128 / 32, 1);
				m_context.ComputeShader.SetUnorderedAccessView(0, null);
				m_context.ComputeShader.SetUnorderedAccessView(1, null);
				m_context.ComputeShader.Set(null);
			}

			if (m_psCompilationResult != null) {
				/*
				m_context.PixelShader.SetSampler(0, m_samplerState);
				m_context.PixelShader.SetShaderResource(0, m_shaderResourceView);
				m_context.PixelShader.Set(m_fullscreenQuadPS);
				m_fullscreenQuad.Render();
				m_context.PixelShader.SetShaderResource(0, null);
				m_context.PixelShader.Set(null);
				*/
			}


			m_spriteBatch.Draw(m_shaderResourceView, new RectangleF(0, 0, 100, 100));
			m_spriteBatch.Render();
		}

		protected override void OnUnload() {
			Utilities.Dispose(ref m_structuredBuffer);
			Utilities.Dispose(ref m_computeShader);
			Utilities.Dispose(ref m_fullscreenQuad);
			Utilities.Dispose(ref m_fullscreenQuadPS);
			Utilities.Dispose(ref m_shaderResourceView);
			Utilities.Dispose(ref m_texture2D);
			Utilities.Dispose(ref m_initShader);
			Utilities.Dispose(ref m_uav);
			Utilities.Dispose(ref m_samplerState);
			Utilities.Dispose(ref m_rtv);
			Utilities.Dispose(ref m_spriteBatch);
		}
	}
}