using System;
using System.Collections.Generic;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	/// <summary>
	/// Overriding and hiding base GUIElement with a bit more handy dandy stuff
	/// </summary>
	public abstract class GUIElement : DXToolKit.GUI.GUIElement, IGUIGriddable {
		public GUIColor GUIColor = GUIColor.Default;
		public GUIBrightness Brightness = GUIBrightness.Normal;
		public GUIBrightness TextBrightness = GUIBrightness.Normal;
		public GUIPadding Padding = new GUIPadding(0);
		public bool MouseIgnoresPadding = true;
		
		// TODO - add a screen bounds offset type thing on base GUIElement to allow for padding to set child element.X and Y to 0
		// TODO - at the moment padding does not position child elements correctly at 0, 0 etc.

		protected sealed override RectangleF MouseScreenBounds {
			get {
				if (MouseIgnoresPadding == false) return ScreenBounds;
				if (Padding.Top > 0 || Padding.Bottom > 0 || Padding.Left > 0 || Padding.Right > 0) {
					return Padding.ResizeRectangle(ScreenBounds);
				}

				return ScreenBounds;
			}
		}

		protected sealed override bool UseClippingBounds => true;

		protected sealed override RectangleF ClippedRenderBounds {
			get {
				var clipbounds = Padding.ResizeRectangle(Bounds);
				return clipbounds;
			}
		}

		protected sealed override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout) {
			// Might not be needed, since clipping bounds will clip away padding
			// Padding.ResizeRectangle(ref bounds);
			OnRender(renderTarget, bounds, textLayout, GUIColorPalette.Current, GUIDrawTools.Current);
		}

		protected virtual void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools) { }
	}
}