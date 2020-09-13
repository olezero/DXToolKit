using System;
using DXToolKit.Engine;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Engine {
	/// <summary>
	/// Base class for elements that have graphics
	/// </summary>
	public abstract class GraphicElement : ActiveElement {
		/// <summary>
		/// Controller to rerun graphics creation
		/// </summary>
		private bool m_runCreateGraphics = true;

		/// <summary>
		/// Rendertexture used to create and store the graphics
		/// </summary>
		private GUIRenderTexture m_renderTexture;

		/// <summary>
		/// Stored padding used so that graphics can be redrawn if needed
		/// </summary>
		private GUIPadding m_graphicsPadding = new GUIPadding(2);


		/// <summary>
		/// Gets or sets a value indicating the padding of the graphics
		/// </summary>
		public GUIPadding GraphicPadding {
			get => m_graphicsPadding;
			set {
				// If they are not the same, rerun graphics
				if (!m_graphicsPadding.Equals(value)) {
					// Set padding
					m_graphicsPadding = value;
					// Trigger graphics update
					m_runCreateGraphics = true;
					// Toggle redraw to run create graphics again with new padding
					ToggleRedraw();
				}
			}
		}

		/// <summary>
		/// Gets the recommended stroke width of this graphic element
		/// </summary>
		public float RecommendedStrokeWidth => GetRecommendedStrokeWidth();

		/// <summary>
		/// Creates the graphics of this element.
		/// This is invoked when the element is created, and when the element is resized
		/// Implementer is responsible to run renderTarget.Begin() and renderTarget.End()
		/// </summary>
		/// <param name="renderTarget">The rendertarget to use for drawing operations</param>
		/// <param name="bounds">Bounds of the graphics</param>
		/// <param name="palette">Color palette for retrieving brushes</param>
		/// <param name="drawTools">Drawing tools to help with drawing</param>
		/// <param name="recommendedStrokeWidth">A recommended stroke width to use while creating the graphics. Tries to fit 8 "strokes" within the smaller of width/height</param>
		protected abstract void CreateGraphics(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools, float recommendedStrokeWidth);

		/// <summary>
		/// Creates the actual graphic for the element
		/// </summary>
		/// <param name="bounds">Graphics bounds</param>
		/// <param name="palette">Color palette for retrieving brushes</param>
		/// <param name="drawTools">Draw tools for helping with drawing (might not really be needed)</param>
		private void CreateGraphics(RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools) {
			// Clear or create the render texture
			if (m_renderTexture == null) {
				m_renderTexture = new GUIRenderTexture(Graphics.Device, (int) bounds.Width, (int) bounds.Height);
			}

			// Direct call, since rendertexture checks if width and height is different before resizing
			m_renderTexture.Resize((int) bounds.Width, (int) bounds.Height);

			// Clear render texture
			m_renderTexture.Clear(Color.Transparent);

			/*
			// Get the smallest size of width / height and use that for calculating stroke width
			var minSize = Math.Min(bounds.Width, bounds.Height);

			// "Smallest size divided by 8" or "1.0F"
			var recommendedStrokeWidth = Math.Max(minSize / 8.0F, 1.0F);
			*/

			// Resize bounds with padding
			GraphicPadding.ResizeRectangle(ref bounds);

			// Round rect
			bounds.X = Mathf.Ceiling(bounds.X);
			bounds.Y = Mathf.Ceiling(bounds.Y);
			bounds.Width = Mathf.Floor(bounds.Width);
			bounds.Height = Mathf.Floor(bounds.Height);

			// Run create graphics
			CreateGraphics(m_renderTexture.RenderTarget, bounds, palette, drawTools, GetRecommendedStrokeWidth());

			// Switch to disable further calls to generate graphics
			m_runCreateGraphics = false;
		}


		/// <inheritdoc />
		protected override void OnBoundsChanged() {
			base.OnBoundsChanged();
			m_runCreateGraphics = true;
		}

		/// <inheritdoc />
		protected override void OnDrawParametersChanged() {
			ToggleGraphicsRedraw();
			base.OnDrawParametersChanged();
		}

		/// <inheritdoc />
		protected sealed override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			// Might have to run create graphics from here since we have access to the draw tools and palette
			if (m_runCreateGraphics) {
				CreateGraphics(drawParameters.Bounds, GUIColorPalette.Current, tools);
				m_runCreateGraphics = false;
			}

			// Call new OnRender with the generated bitmap
			OnRender(tools, ref drawParameters, m_renderTexture.Bitmap);
		}

		/// <summary>
		/// Renders the element
		/// </summary>
		/// <param name="tools">Reference to the gui draw tools</param>
		/// <param name="parameters">Drawing parameters</param>
		/// <param name="graphics">The graphic bitmap to be rendered</param>
		protected virtual void OnRender(GUIDrawTools tools, ref GUIDrawParameters parameters, Bitmap graphics) {
			// Overloaded to include graphics
		}

		/// <summary>
		/// Toggles a redraw of the graphics
		/// </summary>
		public void ToggleGraphicsRedraw() {
			m_runCreateGraphics = true;
			ToggleRedraw();
		}

		/// <summary>
		/// Gets the recommended stroke width used by this graphic element calculated based on width / height
		/// </summary>
		public float GetRecommendedStrokeWidth() {
			// Get the smallest size of width / height and use that for calculating stroke width
			var minSize = Math.Min(Width, Height);
			// "Smallest size divided by 8" or "1.0F"
			return Math.Max(minSize / 8.0F, 1.0F);
		}

		/// <inheritdoc />
		protected override void OnDispose() {
			base.OnDispose();
			Utilities.Dispose(ref m_renderTexture);
		}

		/// <summary>
		/// Draws the element graphics directly using the input draw tools
		/// </summary>
		/// <param name="tools"></param>
		/// <param name="parameters"></param>
		/// <param name="bounds"></param>
		/// <param name="opacity"></param>
		public void DrawDirect(GUIDrawTools tools, GUIDrawParameters parameters, RectangleF bounds, float opacity) {
			if (m_runCreateGraphics) {
				CreateGraphics(bounds, GUIColorPalette.Current, tools);
				m_runCreateGraphics = false;
			}

			parameters.RenderTarget.DrawBitmap(m_renderTexture.Bitmap, bounds, opacity, BitmapInterpolationMode.Linear);
		}
	}
}