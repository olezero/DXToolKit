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
		#region Static

		// Static list over all draw para
		private static Dictionary<Type, GUIDrawParameters> m_defaultParams = new Dictionary<Type, GUIDrawParameters>();

		/// <summary>
		/// Sets the default parameters for a given type T
		/// </summary>
		/// <param name="parameters">The parameters to use as defaults</param>
		/// <typeparam name="T">The type to bind the parameter to</typeparam>
		public static void SetElementDefaults<T>(GUIDrawParameters parameters) where T : GUIElement {
			// Get type
			var type = typeof(T);

			// Check if key exists
			if (!m_defaultParams.ContainsKey(type)) {
				m_defaultParams.Add(type, null);
			}

			// Update params
			m_defaultParams[type] = GUIDrawParameters.DeepCopy(parameters);
		}

		/// <summary>
		/// Setup default parameters based on stored static list of drawing parameters
		/// </summary>
		private void SetDefaultParameters() {
			var type = GetType();
			if (m_defaultParams.ContainsKey(type)) {
				m_drawParameters = GUIDrawParameters.DeepCopy(m_defaultParams[type]);
			}
		}

		#endregion


		#region Fields

		/// <summary>
		/// Private controller for drawing parameters passed to the drawing tools each frame
		/// </summary>
		private GUIDrawParameters m_drawParameters = new GUIDrawParameters();

		/// <summary>
		/// Controller for if this element should inherit style properties from parent element
		/// </summary>
		private bool m_inheritStyle = true;

		/// <summary>
		/// Controller for if this element should pass on its style properties to child elements
		/// </summary>
		private bool m_passOnStyle = true;

		/// <summary>
		/// Controller for passing on font parameters
		/// </summary>
		private bool m_passOnFontParameters = true;

		/// <summary>
		/// Controller for if this element should inherit font parameters from parent element
		/// </summary>
		private bool m_inheritFontParameters = true;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets a value indicating if this element should inherit style parameters from its parent.
		/// Individual parameters can be toggled in StyleInheritance
		/// Default: True
		/// </summary>
		public bool InheritStyle {
			get => m_inheritStyle;
			set => m_inheritStyle = value;
		}

		/// <summary>
		/// Gets or sets a value indicating if this element should pass on style parameters to its children.
		/// Default: True
		/// </summary>
		public bool PassOnStyle {
			get => m_passOnStyle;
			set => m_passOnStyle = value;
		}

		/// <summary>
		/// Gets or sets a value indicating if this element should inherit font parameters from parent element
		/// Individual parameters can be set in StyleInheritance
		/// </summary>
		public bool InheritFontParameters {
			get => m_inheritFontParameters;
			set => m_inheritFontParameters = value;
		}

		/// <summary>
		/// Gets or sets a value indicating if this element should pass on Font Parameters to its children
		/// </summary>
		public bool PassOnFontParameters {
			get => m_passOnFontParameters;
			set => m_passOnFontParameters = value;
		}


		/// <summary>
		/// Gets or sets the foreground color of the element
		/// </summary>
		public GUIColor ForegroundColor {
			get => m_drawParameters.ForegroundColor;
			set {
				StyleInheritance.ForegroundColor = false;

				if (m_drawParameters.ForegroundColor != value) {
					m_drawParameters.ForegroundColor = value;
					ToggleRedraw();
					OnDrawParametersChanged();
				}

				RunPassOnStyle();
			}
		}

		/// <summary>
		/// Gets or sets the background color of the element
		/// </summary>
		public GUIColor BackgroundColor {
			get => m_drawParameters.BackgroundColor;
			set {
				StyleInheritance.BackgroundColor = false;

				if (m_drawParameters.BackgroundColor != value) {
					m_drawParameters.BackgroundColor = value;
					ToggleRedraw();
					OnDrawParametersChanged();
				}

				RunPassOnStyle();
			}
		}

		/// <summary>
		/// Gets or sets the border color of the element
		/// </summary>
		public GUIColor BorderColor {
			get => m_drawParameters.BorderColor;
			set {
				StyleInheritance.BorderColor = false;

				if (m_drawParameters.BorderColor != value) {
					m_drawParameters.BorderColor = value;
					ToggleRedraw();
					OnDrawParametersChanged();
				}

				RunPassOnStyle();
			}
		}

		/// <summary>
		/// Gets or sets the text color of the element
		/// </summary>
		public GUIColor TextColor {
			get => m_drawParameters.TextColor;
			set {
				StyleInheritance.TextColor = false;
				if (m_drawParameters.TextColor != value) {
					m_drawParameters.TextColor = value;
					ToggleRedraw();
					OnDrawParametersChanged();
				}

				RunPassOnStyle();
			}
		}

		/// <summary>
		/// Gets or sets the brightness of the element
		/// </summary>
		public GUIBrightness Brightness {
			get => m_drawParameters.Brightness;
			set {
				StyleInheritance.Brightness = false;

				if (m_drawParameters.Brightness != value) {
					m_drawParameters.Brightness = value;
					ToggleRedraw();
					OnDrawParametersChanged();
				}

				RunPassOnStyle();
			}
		}

		/// <summary>
		/// Gets or sets the text brightness of the element
		/// </summary>
		public GUIBrightness TextBrightness {
			get => m_drawParameters.TextBrightness;
			set {
				StyleInheritance.TextBrightness = false;

				if (m_drawParameters.TextBrightness != value) {
					m_drawParameters.TextBrightness = value;
					ToggleRedraw();
					OnDrawParametersChanged();
				}

				RunPassOnStyle();
			}
		}

		/// <summary>
		/// Gets or sets the border width of the element
		/// </summary>
		public float BorderWidth {
			get => m_drawParameters.BorderWidth;
			set {
				StyleInheritance.BorderWidth = false;

				if (MathUtil.NearEqual(m_drawParameters.BorderWidth, value) == false) {
					m_drawParameters.BorderWidth = value;
					OnDrawParametersChanged();
					ToggleRedraw();
				}

				RunPassOnStyle();
			}
		}

		/// <summary>
		/// Gets or sets the text offset of the element
		/// </summary>
		public Vector2 TextOffset {
			get => m_drawParameters.TextOffset;
			set {
				StyleInheritance.TextOffset = false;

				if (m_drawParameters.TextOffset != value) {
					m_drawParameters.TextOffset = value;
					OnDrawParametersChanged();
					ToggleRedraw();
				}

				RunPassOnStyle();
			}
		}

		/// <summary>
		/// Gets or sets the outer glow parameters of the element
		/// </summary>
		public GlowProperties OuterGlow {
			get => m_drawParameters.OuterGlow;
			set {
				StyleInheritance.OuterGlow = false;
				m_drawParameters.OuterGlow = value;
				RunPassOnStyle();
				OnDrawParametersChanged();
				ToggleRedraw();
			}
		}

		/// <summary>
		/// Gets or sets the inner glow parameters of the element
		/// </summary>
		public GlowProperties InnerGlow {
			get => m_drawParameters.InnerGlow;
			set {
				StyleInheritance.InnerGlow = false;
				m_drawParameters.InnerGlow = value;
				RunPassOnStyle();
				OnDrawParametersChanged();
				ToggleRedraw();
			}
		}

		/// <summary>
		/// Gets or sets the shine opacity of the element
		/// </summary>
		public float ShineOpacity {
			get => m_drawParameters.ShineOpacity;
			set {
				StyleInheritance.ShineOpacity = false;
				m_drawParameters.ShineOpacity = value;
				RunPassOnStyle();
				OnDrawParametersChanged();
				ToggleRedraw();
			}
		}

		/// <summary>
		/// Gets the style inheritance configuration of this element
		/// </summary>
		public StyleInheritance StyleInheritance => m_drawParameters.StyleInheritance;

		/// <summary>
		/// Gets the mouse position in local coordinates
		/// </summary>
		public Vector2 LocalMousePosition => ScreenToLocal(Input.MousePosition);

		#endregion

		#region Constructor

		/// <inheritdoc />
		protected GUIElement() {
			SetDefaultParameters();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Runs a recursive copy of all style parameters that should be inherited
		/// </summary>
		private void RunPassOnStyle() {
			if (!m_passOnStyle) return;
			for (int i = 0; i < ChildElements.Count; i++) {
				if (ChildElements[i] is GUIElement child) {
					// If child does not want new draw parameters, continue
					if (child.m_inheritStyle == false) continue;

					// Copy draw parameters based on DrawParamPropagate
					m_drawParameters.Copy(ref child.m_drawParameters);
					child.ToggleRedraw();
					child.OnDrawParametersChanged();
					child.RunPassOnStyle();
				}
			}
		}

		#endregion

		#region Events

		/// <summary>
		/// Event invoked when a draw parameter is changed
		/// </summary>
		public event Action<GUIDrawParameters> DrawParametersChanged;

		/// <summary>
		/// Invoked when a draw parameter is changed
		/// </summary>
		protected virtual void OnDrawParametersChanged() => DrawParametersChanged?.Invoke(m_drawParameters);

		#endregion

		#region Overrides

		/// <inheritdoc />
		protected override float MinimumWidth => 12;

		/// <inheritdoc />
		protected override float MinimumHeight => 12;

		/// <inheritdoc />
		protected sealed override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout) {
			// Might not be needed, since clipping bounds will clip away padding
			// Padding.ResizeRectangle(ref bounds);
			// OnRender(renderTarget, bounds, textLayout, GUIColorPalette.Current, GUIDrawTools.Current);
			m_drawParameters.TextLayout = textLayout;
			m_drawParameters.RenderTarget = renderTarget;
			m_drawParameters.Bounds = bounds;
			GUIDrawTools.Current.SetParams(ref m_drawParameters);
			OnRender(GUIDrawTools.Current, ref m_drawParameters);
			// OnRender(m_drawParameters.RenderTarget, m_drawParameters.Bounds, m_drawParameters.TextLayout, GUIColorPalette.Current, GUIDrawTools.Current);
		}

		/// <inheritdoc />
		protected sealed override void PostRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout) {
			m_drawParameters.TextLayout = textLayout;
			m_drawParameters.RenderTarget = renderTarget;
			m_drawParameters.Bounds = bounds;
			GUIDrawTools.Current.SetParams(ref m_drawParameters);
			PostRender(GUIDrawTools.Current, ref m_drawParameters);
		}

		/// <summary>
		/// Render the element based on input draw parameters
		/// </summary>
		protected virtual void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) { }

		/// <summary>
		/// Post render after all child elements have been drawn.
		/// Allows for a final pass 
		/// </summary>
		protected virtual void PostRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) { }

		#endregion

		#region TextProps

		/// <inheritdoc />
		protected override void OnChildAppended(GUI.GUIElement parent, GUI.GUIElement child) {
			if (parent.IsDisposing || child.IsDisposing) {
				base.OnChildAppended(parent, child);
				return;
			}

			if (child is GUIElement element) {
				if (m_passOnFontParameters && element.m_inheritFontParameters) {
					element.WithoutInheritance(() => {
						if (element.StyleInheritance.ParagraphAlignment) {
							element.ParagraphAlignment = parent.ParagraphAlignment;
						}

						if (element.StyleInheritance.TextAlignment) {
							element.TextAlignment = parent.TextAlignment;
						}

						if (element.StyleInheritance.WordWrapping) {
							element.WordWrapping = parent.WordWrapping;
						}

						if (element.StyleInheritance.Font) {
							element.Font = parent.Font;
						}

						if (element.StyleInheritance.FontSize) {
							element.FontSize = parent.FontSize;
						}

						if (element.StyleInheritance.FontStyle) {
							element.FontStyle = parent.FontStyle;
						}

						if (element.StyleInheritance.FontWeight) {
							element.FontWeight = parent.FontWeight;
						}

						if (element.StyleInheritance.FontStretch) {
							element.FontStretch = parent.FontStretch;
						}
					});
				}

				if (m_passOnStyle && element.m_inheritStyle) RunPassOnStyle();
			}

			base.OnChildAppended(parent, child);
		}


		/// <summary>
		/// Controller for disabling updates to propagation config
		/// </summary>
		private bool m_ignoreConfig = false;

		/// <summary>
		/// Simple function to set m_dontSetPropagationConfig when action is invoked
		/// </summary>
		private void WithoutInheritance(Action action) {
			m_ignoreConfig = true;
			action?.Invoke();
			m_ignoreConfig = false;
		}

		/// <inheritdoc />
		public sealed override ParagraphAlignment ParagraphAlignment {
			get => base.ParagraphAlignment;
			set {
				if (!m_ignoreConfig) {
					StyleInheritance.ParagraphAlignment = false;
				}

				if (m_passOnFontParameters) {
					foreach (var el in ChildElements) {
						if (el is GUIElement child) {
							if (child.IsDisposing) continue;
							if (child.m_inheritFontParameters && child.StyleInheritance.ParagraphAlignment) {
								child.WithoutInheritance(() => {
									child.ParagraphAlignment = value;
								});
							}
						}
					}
				}

				base.ParagraphAlignment = value;
			}
		}

		/// <inheritdoc />
		public sealed override TextAlignment TextAlignment {
			get => base.TextAlignment;
			set {
				if (!m_ignoreConfig) {
					StyleInheritance.TextAlignment = false;
				}

				if (m_passOnFontParameters) {
					foreach (var el in ChildElements) {
						if (el is GUIElement child) {
							if (child.IsDisposing) continue;
							if (child.m_inheritFontParameters && child.StyleInheritance.TextAlignment) {
								child.WithoutInheritance(() => {
									child.TextAlignment = value;
								});
							}
						}
					}
				}

				base.TextAlignment = value;
			}
		}

		/// <inheritdoc />
		public sealed override WordWrapping WordWrapping {
			get => base.WordWrapping;
			set {
				if (!m_ignoreConfig) {
					StyleInheritance.WordWrapping = false;
				}

				if (m_passOnFontParameters) {
					foreach (var el in ChildElements) {
						if (el is GUIElement child) {
							if (child.IsDisposing) continue;
							if (child.m_inheritFontParameters && child.StyleInheritance.WordWrapping) {
								child.WithoutInheritance(() => {
									child.WordWrapping = value;
								});
							}
						}
					}
				}

				base.WordWrapping = value;
			}
		}

		/// <inheritdoc />
		public sealed override string Font {
			get => base.Font;
			set {
				if (!m_ignoreConfig) {
					StyleInheritance.Font = false;
				}

				if (m_passOnFontParameters) {
					foreach (var el in ChildElements) {
						if (el is GUIElement child) {
							if (child.IsDisposing) continue;
							if (child.m_inheritFontParameters && child.StyleInheritance.Font) {
								child.WithoutInheritance(() => {
									child.Font = value;
								});
							}
						}
					}
				}

				base.Font = value;
			}
		}

		/// <inheritdoc />
		public sealed override int FontSize {
			get => base.FontSize;
			set {
				if (!m_ignoreConfig) {
					StyleInheritance.FontSize = false;
				}

				if (m_passOnFontParameters) {
					foreach (var el in ChildElements) {
						if (el is GUIElement child) {
							if (child.IsDisposing) continue;
							if (child.m_inheritFontParameters && child.StyleInheritance.FontSize) {
								child.WithoutInheritance(() => {
									child.FontSize = value;
								});
							}
						}
					}
				}

				base.FontSize = value;
			}
		}

		/// <inheritdoc />
		public sealed override FontStyle FontStyle {
			get => base.FontStyle;
			set {
				if (!m_ignoreConfig) {
					StyleInheritance.FontStyle = false;
				}

				if (m_passOnFontParameters) {
					foreach (var el in ChildElements) {
						if (el is GUIElement child) {
							if (child.IsDisposing) continue;
							if (child.m_inheritFontParameters && child.StyleInheritance.FontStyle) {
								child.WithoutInheritance(() => {
									child.FontStyle = value;
								});
							}
						}
					}
				}

				base.FontStyle = value;
			}
		}

		/// <inheritdoc />
		public sealed override FontWeight FontWeight {
			get => base.FontWeight;
			set {
				if (!m_ignoreConfig) {
					StyleInheritance.FontWeight = false;
				}

				if (m_passOnFontParameters) {
					foreach (var el in ChildElements) {
						if (el is GUIElement child) {
							if (child.IsDisposing) continue;
							if (child.m_inheritFontParameters && child.StyleInheritance.FontWeight) {
								child.WithoutInheritance(() => {
									child.FontWeight = value;
								});
							}
						}
					}
				}

				base.FontWeight = value;
			}
		}

		/// <inheritdoc />
		public sealed override FontStretch FontStretch {
			get => base.FontStretch;
			set {
				if (!m_ignoreConfig) {
					StyleInheritance.FontStretch = false;
				}

				if (m_passOnFontParameters) {
					foreach (var el in ChildElements) {
						if (el is GUIElement child) {
							if (child.IsDisposing) continue;
							if (child.m_inheritFontParameters && child.StyleInheritance.FontStretch) {
								child.WithoutInheritance(() => {
									child.FontStretch = value;
								});
							}
						}
					}
				}

				base.FontStretch = value;
			}
		}

		#endregion
	}
}