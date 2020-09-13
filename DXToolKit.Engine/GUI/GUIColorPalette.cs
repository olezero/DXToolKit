using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Engine {
	public sealed class GUIColorPalette : DeviceComponent {
		public static GUIColorPalette Current;

		private Dictionary<GUIColor, Dictionary<GUIBrightness, SolidColorBrush>> m_brushes;
		private Dictionary<GUIColor, Dictionary<GUIBrightness, SolidColorBrush>> m_transparentBrushes;
		private Dictionary<GUIColor, LinearGradientBrush> m_gradiantBrushes;
		private Dictionary<GUIColor, RadialGradientBrush> m_radialBrushes;

		private List<GradientStopCollection> m_stopCollections;

		private Dictionary<DashStyle, StrokeStyle> m_strokeStyles;
		private SolidColorBrush m_lerpBrush;

		public Dictionary<DashStyle, StrokeStyle> StrokeStyles => m_strokeStyles;

		public GUIColorPalette(GraphicsDevice device, GUIColorPaletteDescription description) : base(device) {
			// Create brushes
			CreateBrushes(description);
			CreateTransparentBrushes(description);
			CreateGradientBrushes(description);

			m_lerpBrush = new SolidColorBrush(m_device, Color.White);
			// Create stroke styles
			CreateStrokeStyles();

			// Set current to the first created palette
			if (Current == null) {
				Current = this;
			}
		}

		protected override void OnDispose() {
			ReleaseStrokeStyles();
			ReleaseBrushes();
			ReleaseTransparentBrushes();
			ReleaseGradientBrushes();
			Utilities.Dispose(ref m_lerpBrush);
		}

		private void CreateStrokeStyles() {
			ReleaseStrokeStyles();
			m_strokeStyles = new Dictionary<DashStyle, StrokeStyle>();
			foreach (DashStyle dash in Enum.GetValues(typeof(DashStyle))) {
				if (dash == DashStyle.Custom) {
					continue;
				}

				m_strokeStyles.Add(dash, new StrokeStyle(m_device.Factory, new StrokeStyleProperties {
					DashStyle = dash,
				}));
			}
		}

		public SolidColorBrush this[GUIColor color, GUIBrightness brightness, bool transparent = false] => transparent ? m_transparentBrushes[color][brightness] : m_brushes[color][brightness];

		public SolidColorBrush GetBrush(GUIColor color, GUIBrightness brightness) {
			return this[color, brightness, false];
		}

		public SolidColorBrush GetTransparentBrush(GUIColor color, GUIBrightness brightness) {
			return this[color, brightness, true];
		}

		public LinearGradientBrush GetGradientBrush(GUIColor color) {
			return m_gradiantBrushes[color];
		}

		public RadialGradientBrush GetRadialGradientBrush(GUIColor color) {
			return m_radialBrushes[color];
		}

		public SolidColorBrush LerpedBrush(SolidColorBrush from, SolidColorBrush to, float amount) {
			var fromColor = from.Color;
			var toColor = to.Color;

			m_lerpBrush.Color = Color.Lerp(
				new Color(fromColor.R, fromColor.G, fromColor.B, fromColor.A),
				new Color(toColor.R, toColor.G, toColor.B, toColor.A),
				Mathf.Clamp(amount, 0, 1)
			);

			return m_lerpBrush;
		}

		public SolidColorBrush LerpedBrush(GUIColor colorFrom, GUIColor colorTo, GUIBrightness brightnessFrom, GUIBrightness brightnessTo, float amount, bool transparent = false) {
			var fromBrush = this[colorFrom, brightnessFrom, transparent];
			var toBrush = this[colorTo, brightnessTo, transparent];
			return LerpedBrush(fromBrush, toBrush, amount);
		}

		private void CreateBrushes(GUIColorPaletteDescription description) {
			ReleaseBrushes();
			m_brushes = new Dictionary<GUIColor, Dictionary<GUIBrightness, SolidColorBrush>>();
			foreach (GUIColor guiColor in Enum.GetValues(typeof(GUIColor))) {
				m_brushes.Add(guiColor, CreateBrushes(description[guiColor], description));
			}
		}

		private void CreateTransparentBrushes(GUIColorPaletteDescription description) {
			ReleaseTransparentBrushes();
			m_transparentBrushes = new Dictionary<GUIColor, Dictionary<GUIBrightness, SolidColorBrush>>();
			foreach (GUIColor guiColor in Enum.GetValues(typeof(GUIColor))) {
				m_transparentBrushes.Add(guiColor, CreateBrushes(description[guiColor], description, description.TransparentAlpha));
			}
		}

		private void CreateGradientBrushes(GUIColorPaletteDescription description) {
			ReleaseGradientBrushes();
			m_gradiantBrushes = new Dictionary<GUIColor, LinearGradientBrush>();
			m_radialBrushes = new Dictionary<GUIColor, RadialGradientBrush>();
			m_stopCollections = new List<GradientStopCollection>();
			foreach (GUIColor guiColor in Enum.GetValues(typeof(GUIColor))) {
				m_gradiantBrushes.Add(guiColor, CreateGradientBrush(description[guiColor]));
				m_radialBrushes.Add(guiColor, CreateRadialGradientBrush(description[guiColor]));
			}
		}

		private LinearGradientBrush CreateGradientBrush(Color baseColor) {
			var linearGradientBrushProperties = new LinearGradientBrushProperties {
				StartPoint = Vector2.Zero,
				EndPoint = Vector2.One
			};

			var fullAlphaColor = baseColor;
			var noAlphaColor = baseColor;
			fullAlphaColor.A = 255;
			noAlphaColor.A = 0;

			var gradientStopCollection = new GradientStopCollection(m_renderTarget, new[] {
				new GradientStop {
					Color = fullAlphaColor,
					Position = 0.0F
				},
				new GradientStop {
					Color = noAlphaColor,
					Position = 1.0F,
				},
			}, ExtendMode.Mirror);
			m_stopCollections.Add(gradientStopCollection);
			return new LinearGradientBrush(m_device, linearGradientBrushProperties, gradientStopCollection);
		}

		private RadialGradientBrush CreateRadialGradientBrush(Color baseColor) {
			var radialGradientBrushProperties = new RadialGradientBrushProperties {
				Center = Vector2.Zero,
				RadiusX = 1,
				RadiusY = 1,
				GradientOriginOffset = Vector2.Zero,
			};

			var fullAlphaColor = baseColor;
			var noAlphaColor = baseColor;
			fullAlphaColor.A = 255;
			noAlphaColor.A = 0;

			var gradientStopCollection = new GradientStopCollection(m_renderTarget, new[] {
				new GradientStop {
					Color = fullAlphaColor,
					Position = 0.0F
				},
				new GradientStop {
					Color = noAlphaColor,
					Position = 1.0F,
				},
			}, ExtendMode.Mirror);
			m_stopCollections.Add(gradientStopCollection);
			return new RadialGradientBrush(m_device, radialGradientBrushProperties, gradientStopCollection);
		}


		private Dictionary<GUIBrightness, SolidColorBrush> CreateBrushes(Color baseColor, GUIColorPaletteDescription description, float alpha = 1.0F) {
			var black = Color.Black;
			var white = Color.White;
			var byteAlpha = (byte) (alpha * 255.0F);

			white.A = byteAlpha;
			black.A = byteAlpha;
			baseColor.A = byteAlpha;

			var result = new Dictionary<GUIBrightness, SolidColorBrush> {
				{GUIBrightness.Darkest, new SolidColorBrush(m_device, Color.Lerp(baseColor, black, description.DarkestStrength))},
				{GUIBrightness.Dark, new SolidColorBrush(m_device, Color.Lerp(baseColor, black, description.DarkStrength))},
				{GUIBrightness.Normal, new SolidColorBrush(m_device, baseColor)},
				{GUIBrightness.Bright, new SolidColorBrush(m_device, Color.Lerp(baseColor, white, description.BrightStrength))},
				{GUIBrightness.Brightest, new SolidColorBrush(m_device, Color.Lerp(baseColor, white, description.BrightestStrength))}
			};
			return result;
		}

		private void ReleaseBrushes() {
			if (m_brushes != null) {
				foreach (var brushlib in m_brushes) {
					if (brushlib.Value != null) {
						foreach (var brush in brushlib.Value) {
							brush.Value?.Dispose();
						}

						brushlib.Value?.Clear();
					}
				}

				m_brushes?.Clear();
			}
		}


		private void ReleaseTransparentBrushes() {
			if (m_transparentBrushes != null) {
				foreach (var brushlib in m_transparentBrushes) {
					if (brushlib.Value != null) {
						foreach (var brush in brushlib.Value) {
							brush.Value?.Dispose();
						}

						brushlib.Value?.Clear();
					}
				}

				m_transparentBrushes?.Clear();
			}
		}

		private void ReleaseGradientBrushes() {
			if (m_gradiantBrushes != null) {
				foreach (var brush in m_gradiantBrushes) {
					brush.Value?.Dispose();
				}

				m_gradiantBrushes?.Clear();
			}

			if (m_stopCollections != null) {
				foreach (var stopCollection in m_stopCollections) {
					stopCollection?.Dispose();
				}

				m_stopCollections?.Clear();
			}

			if (m_radialBrushes != null) {
				foreach (var brush in m_radialBrushes) {
					brush.Value?.Dispose();
				}

				m_radialBrushes?.Clear();
			}
		}


		private void ReleaseStrokeStyles() {
			if (m_strokeStyles != null) {
				foreach (var strokeStyle in m_strokeStyles) {
					strokeStyle.Value?.Dispose();
				}
			}

			m_strokeStyles?.Clear();
			m_strokeStyles = null;
		}

		public void DebugRender(RenderTarget renderTarget, RectangleF destination) {
			renderTarget.BeginDraw();
			var widthPer = destination.Width / 5.0F;
			var heightPer = destination.Height / 6.0F;

			var xOffset = destination.X;
			var yOffset = destination.Y;

			foreach (var brushLib in m_brushes) {
				foreach (var brush in brushLib.Value) {
					renderTarget.FillRectangle(new RectangleF(xOffset, yOffset, widthPer, heightPer), brush.Value);
					xOffset += widthPer;
				}

				yOffset += heightPer;
				xOffset = destination.X;
			}

			renderTarget.EndDraw();
		}
	}
}