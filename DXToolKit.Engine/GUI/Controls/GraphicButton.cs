using System;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	public abstract class GraphicButton : ActiveElement {
		private bool m_runCreateGraphics = true;

		private GUIRenderTexture m_renderTexture;

		protected abstract void CreateGraphics(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools, float recommendedStrokeWidth);

		private void CreateGraphics(RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools) {
			// Clear or create the render texture
			if (m_renderTexture == null) {
				m_renderTexture = new GUIRenderTexture(Graphics.Device, (int) bounds.Width, (int) bounds.Height);
			}

			// Direct call, since rendertexture checks if width and height is different before resizing
			m_renderTexture.Resize((int) bounds.Width, (int) bounds.Height);

			// Clear render texture
			m_renderTexture.Clear(Color.Transparent);

			var recommendedStrokeWidth = Math.Max(bounds.Width / 8.0F, 1.0F);

			// Run create graphics
			CreateGraphics(m_renderTexture.RenderTarget, bounds, palette, drawTools, recommendedStrokeWidth);

			// Switch to disable further calls to generate graphics
			m_runCreateGraphics = false;
		}

		protected override void OnBoundsChanged() {
			base.OnBoundsChanged();
			m_runCreateGraphics = true;
		}

		protected sealed override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools) {
			// Might have to run create graphics from here since we have access to the draw tools and palette
			if (m_runCreateGraphics) {
				CreateGraphics(bounds, palette, drawTools);
				m_runCreateGraphics = false;
			}

			// Call new OnRender with the generated bitmap
			OnRender(renderTarget, bounds, textLayout, palette, drawTools, m_renderTexture.Bitmap);
		}

		protected virtual void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools, Bitmap graphics) {
			// Could just do a simple render here
			var targetBrightness = Brightness;
			var iconOffset = new Vector2();

			if (MouseHovering) {
				if (IsMousePressed) {
					targetBrightness = GUIBrightness.Brightest;
					iconOffset += 1;
				} else {
					targetBrightness = GUIBrightness.Bright;
				}
			}


			drawTools.Rectangle(renderTarget, bounds, palette, ForegroundColor, targetBrightness);
			bounds.Location += iconOffset;
			renderTarget.DrawBitmap(graphics, bounds, 1.0F, BitmapInterpolationMode.Linear);
		}


		protected override void OnDispose() {
			base.OnDispose();
			Utilities.Dispose(ref m_renderTexture);
		}
	}
}