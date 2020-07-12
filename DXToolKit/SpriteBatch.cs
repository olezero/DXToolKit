using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace DXToolKit {
	public class SpriteBatch : DeviceComponent {
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct SpriteBufferType {
			public Vector4 Destination;
			public float Depth;
			public float Rotation;
			public Vector2 Origin;
			public Vector4 Source;
			public Vector4 Color;
		}

		private List<ShaderResourceView> m_spriteTextures;
		private List<SpriteBufferType> m_sprites;
		private VertexBuffer<SpriteBufferType> m_spriteBuffer;
		private ConstantBuffer<Matrix> m_matrixBuffer;

		private InputLayout m_inputLayout;
		private VertexShader m_vertexShader;
		private PixelShader m_pixelShader;
		private GeometryShader m_geometryShader;

		private SamplerState m_sampler;
		private BlendState m_blendState;

		public SpriteBatch(GraphicsDevice device) : base(device) {
			m_spriteTextures = new List<ShaderResourceView>();
			m_sprites = new List<SpriteBufferType>();
			m_spriteBuffer = new VertexBuffer<SpriteBufferType>(m_device, 1);

			var samplerDesc = SamplerStateDescription.Default();
			samplerDesc.Filter = Filter.MinMagMipLinear;
			samplerDesc.AddressW = TextureAddressMode.Clamp;
			samplerDesc.AddressU = TextureAddressMode.Clamp;
			samplerDesc.AddressV = TextureAddressMode.Clamp;
			m_sampler = new SamplerState(m_device, samplerDesc);

			var blendDesc = BlendStateDescription.Default();
			blendDesc.RenderTarget[0].IsBlendEnabled = true;
			for (int i = 0; i < blendDesc.RenderTarget.Length; i++) {
				blendDesc.RenderTarget[i].IsBlendEnabled = true;
				blendDesc.RenderTarget[i].SourceBlend = BlendOption.SourceAlpha;
				blendDesc.RenderTarget[i].DestinationBlend = BlendOption.InverseSourceAlpha;
			}

			m_blendState = new BlendState(m_device, blendDesc);
			m_matrixBuffer = new ConstantBuffer<Matrix>(m_device);

			LoadShaders();
		}

		public void Draw(ShaderResourceView texture, RectangleF destination, float? rotation = null,
			float? depth = null, Vector2? origin = null, RectangleF? source = null, Color? color = null) {
			m_spriteTextures.Add(texture);

			var vertex = new SpriteBufferType {
				Destination = new Vector4(destination.X, destination.Y, destination.Width, destination.Height),
			};

			if (depth != null && depth > 0) {
				vertex.Depth = depth.Value;
			}

			if (rotation != null && rotation > 0) {
				vertex.Rotation = Mathf.DegToRad(rotation.Value);
			}

			if (origin != null) {
				vertex.Origin = origin.Value;
			}

			if (source != null) {
				vertex.Source.X = source.Value.X;
				vertex.Source.Y = source.Value.Y;
				vertex.Source.Z = source.Value.Width;
				vertex.Source.W = source.Value.Height;
			}

			if (color != null) {
				var clr = (Color) color;
				vertex.Color = clr.ToVector4();
			} else {
				vertex.Color = new Vector4(1, 1, 1, 1);
			}

			m_sprites.Add(vertex);
		}

		/*
		public void Draw(ShaderResourceView texture, Transform2D transform, Size2F destinationSize,
			RectangleF sourceRectangle) { }
			*/

		public void Render(DXCamera camera, Matrix? transform = null, SamplerState sampler = null, BlendState blend = null) {
			// Check if there are sprites to render
			if (m_sprites.Count > 0) {
				// Write those sprites to the GPU sprite buffer
				m_spriteBuffer.WriteRange(m_sprites.ToArray());

				// Set vertex buffer
				m_context.InputAssembler.SetVertexBuffers(0, m_spriteBuffer);

				// Update matrix buffer with view projection (add a world matrix here)
				m_matrixBuffer.Write(Matrix.Transpose((transform ?? Matrix.Identity) * camera.ViewProjection));

				// Set input layout and topology to point list, since geometry shader handles quad generation
				m_context.InputAssembler.InputLayout = m_inputLayout;
				m_context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;

				// Set shaders
				m_context.PixelShader.Set(m_pixelShader);
				m_context.VertexShader.Set(m_vertexShader);
				m_context.GeometryShader.Set(m_geometryShader);

				// Set shader variables
				m_context.GeometryShader.SetConstantBuffer(0, m_matrixBuffer);
				m_context.PixelShader.SetSampler(0, sampler ?? m_sampler);

				// Set blend state
				if (blend != null) {
					m_context.OutputMerger.BlendState = blend;
					m_context.OutputMerger.BlendFactor = Color.Transparent;
					m_context.OutputMerger.BlendSampleMask = 0x0F;
				} else {
					m_context.OutputMerger.BlendState = m_blendState;
					m_context.OutputMerger.BlendFactor = Color.Transparent;
					m_context.OutputMerger.BlendSampleMask = 0x0F;
				}

				// Loop through all sprite textures
				for (int i = 0; i < m_spriteTextures.Count; i++) {
					// Set texture in pixel shader
					m_context.PixelShader.SetShaderResource(0, m_spriteTextures[i]);
					m_context.GeometryShader.SetShaderResources(0, m_spriteTextures[i]);

					// Draw sprite at current index
					m_context.Draw(1, i);
				}
			}

			// Clear texture buffer (it only contains references to textures, so no need to dispose)I
			m_spriteTextures.Clear();

			// Clear sprite info buffer
			m_sprites.Clear();
		}

		private void LoadShaders() {
			const ShaderFlags shaderFlags = ShaderFlags.OptimizationLevel3;
			var vsByteCode = ShaderBytecode.Compile(SHADER_SOURCE, "VS", "vs_5_0", shaderFlags);
			var psByteCode = ShaderBytecode.Compile(SHADER_SOURCE, "PS", "ps_5_0", shaderFlags);
			var gsByteCode = ShaderBytecode.Compile(SHADER_SOURCE, "GS", "gs_5_0", shaderFlags);

			m_vertexShader = new VertexShader(m_device, vsByteCode);
			m_pixelShader = new PixelShader(m_device, psByteCode);
			m_geometryShader = new GeometryShader(m_device, gsByteCode);
			m_inputLayout = new InputLayout(m_device, vsByteCode, new[] {
				// Destination rectangle
				new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0),
				// Depth
				new InputElement("SCALAR", 0, Format.R32_Float, 0),
				// Rotation
				new InputElement("SCALAR", 1, Format.R32_Float, 0),
				// Origin
				new InputElement("TEXCOORD", 1, Format.R32G32_Float, 0),
				// Source rectangle
				new InputElement("TEXCOORD", 2, Format.R32G32B32A32_Float, 0),
				// Color
				new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 0),
			});
		}

		protected override void OnDispose() {
			Utilities.Dispose(ref m_spriteBuffer);
			Utilities.Dispose(ref m_sampler);
			Utilities.Dispose(ref m_matrixBuffer);
			Utilities.Dispose(ref m_vertexShader);
			Utilities.Dispose(ref m_geometryShader);
			Utilities.Dispose(ref m_pixelShader);
			Utilities.Dispose(ref m_inputLayout);
			Utilities.Dispose(ref m_blendState);
		}

		private const string SHADER_SOURCE = @"
cbuffer MatrixBuffer 			: register(b0) {
	float4x4 g_worldViewProj;
}

Texture2D g_texture				: register(t0);
SamplerState g_sampler		 	: register(s0);

struct VSInn {
	float4 position			: POSITION;
	float depth 			: SCALAR0;
	float rotation			: SCALAR1;
	float2 Origin 			: TEXCOORD1;
	float4 OriginRect 		: TEXCOORD2;
	float4 color			: COLOR;
};

struct GSInn {
	float2 position 		: POSITION;
	float depth				: SCALAR0;
	float rotation      	: SCALAR1;
	float2 size 			: TEXCOORD0;
	float2 Origin 			: TEXCOORD1;
	float2 UVOrigin 		: TEXCOORD2;
	float2 UVSize			: TEXCOORD3;
	float4 color			: COLOR;
};

struct PSInn {
	float4 position		: SV_Position;
	float2 uv			: TEXCOORD;
	float4 color		: COLOR;
};

// Rotates a given point around a center by a given angle in radians
float2 rotatePoint(in float2 pt, in float2 center, in float angle) {
	float rotatedX = cos(angle) * (pt.x - center.x) - sin(angle) * (pt.y - center.y) + center.x;
	float rotatedY = sin(angle) * (pt.x - center.x) + cos(angle) * (pt.y - center.y) + center.y;
	return float2(rotatedX, rotatedY);
};

GSInn VS(VSInn input) {
	GSInn output = (GSInn)0;
	// Destination rectangle
	output.position = input.position.xy;
	output.size = input.position.zw;
	// Scalars
	output.depth = input.depth;
	output.rotation = input.rotation;
	// Origin
	output.Origin = input.Origin;
	// Source rectangle
	output.UVOrigin = input.OriginRect.xy;
	output.UVSize = input.OriginRect.zw;
	// Color
	output.color = input.color;
	return output;
};

[maxvertexcount(4)]
void GS(point GSInn input[1], inout TriangleStream<PSInn> stream) {
	// Create 2 triangles from corners
	const uint indices[4] = { 0, 1, 3, 2 };
	const uint uvIndices[4] = { 0, 1, 2, 3 };

	// Get 4 corners of quad
	float2 vertices[4] = {
		// Top Left
		input[0].position + float2(0, input[0].size.y),
		// Top Right
		input[0].position + float2(input[0].size.x, input[0].size.y),
		// Bottom Right
		input[0].position + float2(input[0].size.x, 0),
		// Bottom Left
		input[0].position,
	};

	float texWidth = 0;
	float texHeight = 0;
	float2 pxPerUV = float2(1, 1);

	// Get texture dimensions for use when offsetting source
	g_texture.GetDimensions(texWidth, texHeight);
	if (texWidth > 0 && texHeight > 0) {
		pxPerUV.x = 1.0F / texWidth;
		pxPerUV.y = 1.0F / texHeight;
	}

	// UV origin converted from pixel space to UV space
	float2 UVOrigin = input[0].UVOrigin * pxPerUV;
	// Size of UV map converted from pixel space to UV space
	float2 UVSize = input[0].UVSize * pxPerUV;

	// Not set, so get whole Texture
	if (input[0].UVSize.x == 0 || input[0].UVSize.y == 0) {
		UVSize.x = 1;
		UVSize.y = 1;
	}

	// Calcualte uvs based on origin + size
	float2 uvs[4] = {
		// Top Left
		UVOrigin + float2(0, 0),
		// Top Right
		UVOrigin + float2(UVSize.x, 0),
		// Bottom Right
		UVOrigin + float2(UVSize.x, UVSize.y),
		// Bottom Left
		UVOrigin + float2(0, UVSize.y),
	};

	// Calculate center of quad/rotation based on input origin
	float2 center = input[0].position + input[0].Origin;

	PSInn output = (PSInn)0;
	// 4 vertices in a quad
	for(uint i = 0; i < 4; i++) {
		// Get index based on indices array
		uint index = indices[i];
		// Get rotated point based on rotation offset by origin
		float2 rotatedPoint = rotatePoint(vertices[index], center, input[0].rotation) - input[0].Origin;
		// Create float 4 with Z value from input depth
		output.position = float4(rotatedPoint, input[0].depth, 1.0F);
		// Multiply position with input world view projection matrix
		output.position = mul(output.position, g_worldViewProj);
		// Get uv matching up with index
		output.uv = uvs[uvIndices[index]];
		// Set color for pixel shader
		output.color = input[0].color;
		// Append vertex to output stream
		stream.Append(output);
	}
}

float4 PS(PSInn input) : SV_Target {
	return g_texture.Sample(g_sampler, input.uv) * input.color;
}
";
	}
}