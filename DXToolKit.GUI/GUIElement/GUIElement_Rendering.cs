using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.GUI {
	public partial class GUIElement {
		#region Render

		// This does not really need to be called every update loop. Can just be called when needed
		// Although that might be better, since at update we might receive 10 different calls that needs a redraw, and we only want to draw the element max 1 time per frame
		internal void Render(RenderTarget target, GUISystem guiSystem) {
			// Render the drawn texture to input rendertarget
			if (m_enabled && m_visible) {
				RenderBitmap(target, guiSystem);
			}
		}

		/// <summary>
		/// Renders this element and all its child elements that needs to be redrawn to its internal render texture
		/// </summary>
		private void Redraw(GUISystem guiSystem) {
			if (m_enabled && m_visible && m_bounds.Width > 0 && m_bounds.Height > 0) {
				// Make sure render texture is initialized
				if (m_renderTexture == null) {
					// Create render texture
					m_renderTexture = new GUIRenderTexture(guiSystem.GraphicsDevice, (int) m_bounds.Width, (int) m_bounds.Height);
					// Add rendertexture to dispose pool
					guiSystem.ToDispose(m_renderTexture);
				}

				// Make sure rendertexture is the correct size
				if (m_resizeRenderTexture) {
					m_renderTexture.Resize((int) m_bounds.Width, (int) m_bounds.Height);
					m_resizeRenderTexture = false;
				}

				// Set transform
				m_renderTexture.RenderTarget.Transform = Matrix3x2.Translation(m_renderOffset) * RenderTransform;

				// Begin drawing
				m_renderTexture.RenderTarget.BeginDraw();

				// Clear rendertexture
				m_renderTexture.RenderTarget.Clear(Color.Transparent);

				// Render bounds has the same width and height as local bounds but are not offset on the x and y axis
				var renderBounds = m_bounds;

				// Offset renderBounds by render offset so that the bounds are still inside the element bounds.
				renderBounds.X = -m_renderOffset.X;
				renderBounds.Y = -m_renderOffset.Y;

				if (UseClippingBounds) {
					var clipBounds = ClippedRenderBounds;
					// Remove bounds since clipping is 0 based and not screen based. Also add back render offset since we dont want the clipping to be offset by the render offset.
					clipBounds.X -= m_bounds.X + m_renderOffset.X;
					clipBounds.Y -= m_bounds.Y + m_renderOffset.Y;
					m_renderTexture.RenderTarget.PushAxisAlignedClip(clipBounds, AntialiasMode.Aliased);
				}

				// Render this element
				OnRender(m_renderTexture.RenderTarget, renderBounds, m_guiText.GetTextLayout(guiSystem.GraphicsDevice.Factory, m_text, renderBounds.Width, renderBounds.Height));

				// Run events
				OnRedraw?.Invoke();

				// Render all children
				foreach (var childElement in m_childElements) {
					// Render child to this elements render texture
					childElement.RenderBitmap(m_renderTexture.RenderTarget, guiSystem);
				}

				// Run post render
				PostRender(m_renderTexture.RenderTarget, renderBounds, m_guiText.GetTextLayout(guiSystem.GraphicsDevice.Factory, m_text, renderBounds.Width, renderBounds.Height));

				if (UseClippingBounds) {
					m_renderTexture.RenderTarget.PopAxisAlignedClip();
				}

				// End drawing
				m_renderTexture.RenderTarget.EndDraw();

				// Toggle redraw
				m_redraw = false;

				// Increment redraw count
				RedrawCount++;
				ElementRedraw?.Invoke(this);
			}
		}

		/// <summary>
		/// Draws this elements built inn render texture to the input render target
		/// </summary>
		/// <param name="renderTarget">Render target to draw the bitmap to</param>
		/// <param name="guiSystem"></param>
		private void RenderBitmap(RenderTarget renderTarget, GUISystem guiSystem) {
			if (m_enabled && m_visible) {
				// If we need to redraw, redraw.
				if (m_redraw) Redraw(guiSystem);

				// TODO 
				// Possibiibibibility, could round down or up based on remainder
				var renderBounds = m_bounds;

				/*
				renderBounds.X = Mathf.Floor(renderBounds.X);
				renderBounds.Y = Mathf.Floor(renderBounds.Y);
				renderBounds.Width = Mathf.Floor(renderBounds.Width);
				renderBounds.Height = Mathf.Floor(renderBounds.Height);
				*/

				// Then render our complete texture to callers render target
				renderTarget.DrawBitmap(m_renderTexture.Bitmap, renderBounds, m_opacity, BitmapInterpolationMode.NearestNeighbor);
			}
		}

		/// <summary>
		/// Toggles a redraw of this element and all parent elements
		/// </summary>
		public void ToggleRedraw() {
			// If this element needs a redraw, all parents also need to redraw.
			m_parentElement?.ToggleRedraw();
			// NOTE: Might be a good idea to run toggle redraw all the way back to root node no matter what. Since the recurse might stop to early. For instance Root does not have Redraw=True while child->child->child has RedrawTrue
			// This method is far from costly anyways..

			// Should not need to toggle redraw if its already been toggled
			// if (m_redraw) return; // TODO - Figure out if this check breaks anything
			// Set redraw to true
			m_redraw = true;
		}

		/// <summary>
		/// Toggles a resize of the stored render texture on this element
		/// </summary>
		public void ToggleResize() {
			m_resizeRenderTexture = true;
		}

		#endregion
	}
}