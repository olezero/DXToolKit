using System;
using DXToolKit;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.DirectWrite;


namespace DXToolKit.Engine {
	// ReSharper disable MemberInitializerValueIgnored
	/// <summary>
	/// Basic draggable window element.
	/// Node: All child element should be appended to Window.Body and not the window directly
	/// </summary>
	public class Window : GUIElement {
		#region Class Definitions

		/// <summary>
		/// Window header element
		/// </summary>
		public class WindowHeader : GUIElement {
			private bool m_ignoreAppendException = true;
			private CloseButton m_closeButton;

			/// <summary>
			/// Close button used by the window
			/// </summary>
			public CloseButton CloseButton => m_closeButton;

			/// <summary>
			/// Creates a new window header
			/// </summary>
			public WindowHeader() {
				Text = "Window";
				TextOffset = new Vector2(4, 0);
				// FontWeight = FontWeight.Bold;
				m_closeButton = Append(new CloseButton() {
					SinkOnPress = false,
				});
				m_ignoreAppendException = false;
				Draggable = true;
				PositionChildren();
			}


			/// <inheritdoc />
			protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
				tools.Background.Rectangle();
				tools.Background.BevelBorder();
				tools.Text();
			}

			/// <inheritdoc />
			protected override void OnChildAppended(DXToolKit.GUI.GUIElement parent, DXToolKit.GUI.GUIElement child) {
				if (!m_ignoreAppendException) {
					throw new Exception("Cannot append directly to header");
				}

				base.OnChildAppended(parent, child);
			}

			/// <inheritdoc />
			protected override void OnBoundsChangedDirect() {
				PositionChildren();
				base.OnBoundsChangedDirect();
			}

			private void PositionChildren() {
				m_closeButton.Width = Height - BorderWidth * 2;
				m_closeButton.Height = Height - BorderWidth * 2;
				m_closeButton.Top = Top + BorderWidth;
				m_closeButton.Right = Right - BorderWidth;
			}
		}

		/// <summary>
		/// Empty GUIElement to store window child elements
		/// </summary>
		public class WindowBody : GUIElement { }

		#endregion

		private bool m_ignoreAppendException = true;
		private WindowHeader m_header;
		private WindowBody m_body;
		private float m_headerHeight = 20;
		private bool m_useInnerBorder = true;
		private bool m_useInnerBorderShadow = true;
		private float m_innerBorderSize = 3.0F;

		/// <summary>
		/// Controller for where dragging started
		/// </summary>
		private Vector2 m_dragStartLocation;

		/// <summary>
		/// Gets or sets a value indicating if the window is draggable
		/// </summary>
		public new bool Draggable = true;

		/// <summary>
		/// Gets a reference to the header element
		/// </summary>
		public WindowHeader Header => m_header;

		/// <summary>
		/// Gets a reference to the body element
		/// </summary>
		public WindowBody Body => m_body;

		/// <summary>
		/// Gets a reference to the close button element on the header
		/// </summary>
		public CloseButton CloseButton => m_header.CloseButton;

		/// <summary>
		/// Event raised when the window is being closed
		/// </summary>
		public event Action Closing;

		/// <summary>
		/// Event raised when the window is being opened
		/// </summary>
		public event Action Opening;

		/// <summary>
		/// Gets or sets a value indicating if the window should be disposed of when closed.
		/// Useful for quick dialog boxes or message popups
		/// </summary>
		public bool DisposeOnClose = false;

		/// <summary>
		/// Gets or sets a value indicating if a inner border should be drawn
		/// </summary>
		public bool UseInnerBorder {
			get => m_useInnerBorder;
			set {
				if (m_useInnerBorder != value) {
					m_useInnerBorder = value;
					ToggleRedraw();
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating if there should be a shadow effect on the inner border
		/// </summary>
		public bool UseInnerBorderShadow {
			get => m_useInnerBorderShadow;
			set {
				if (m_useInnerBorderShadow != value) {
					m_useInnerBorderShadow = value;
					ToggleRedraw();
				}
			}
		}

		/// <summary>
		/// Gets or sets a value that controls the distance from the outer bounds to the inner border
		/// </summary>
		public float InnerBorderSize {
			get => m_innerBorderSize;
			set {
				if (MathUtil.NearEqual(m_innerBorderSize, value) == false) {
					m_innerBorderSize = value;
					ToggleRedraw();
				}
			}
		}

		/// <summary>
		/// Gets or sets a value that controls the height of the header element
		/// </summary>
		public float HeaderHeight {
			get => m_headerHeight;
			set {
				m_headerHeight = value;
				PositionChildren();
			}
		}

		/// <summary>
		/// Create a new window
		/// </summary>
		public Window() {
			ConstrainToParent = true;
			m_body = Append(new WindowBody {
				PassOnStyle = false,
				PassOnFontParameters = false,
			});

			m_header = Append(new WindowHeader());
			m_ignoreAppendException = false;
			Width = 12 * 12;
			Height = 12 * 12;
			CanReceiveMouseInput = false;
			TextAlignment = TextAlignment.Leading;
			ParagraphAlignment = ParagraphAlignment.Center;
			InnerGlow.Opacity = 0.3F;

			m_header.DragStart += () => {
				m_dragStartLocation = m_header.LocalMousePosition;
			};

			m_header.Drag += () => {
				if (Draggable) {
					Location = Input.MousePosition - m_dragStartLocation;
				}
			};

			CloseButton.Click += args => {
				Close();
			};

			// Lock header
			m_header.BoundsChangedDirect += () => {
				m_header.X = 0;
				m_header.Y = 0;
				m_headerHeight = m_header.Height;
				Width = m_header.Width;
				PositionChildren();
			};

			// Lock body
			m_body.BoundsChangedDirect += () => {
				Width = m_body.Width;
				Height = m_body.Height + m_headerHeight;
				m_body.X = 0;
				m_body.Y = m_headerHeight;
				PositionChildren();
			};
		}

		/// <inheritdoc />
		protected override void OnTextChanged(string text) {
			m_header.Text = text;
			base.OnTextChanged(text);
		}

		/// <inheritdoc />
		protected override void OnChildAppended(DXToolKit.GUI.GUIElement parent, DXToolKit.GUI.GUIElement child) {
			if (!m_ignoreAppendException) {
				throw new Exception("Cannot append directly to window, append to Window.Body");
			}

			base.OnChildAppended(parent, child);
		}

		/// <inheritdoc />
		protected override void OnBoundsChangedDirect() {
			if (Width < 12 * 8) {
				Width = 12 * 8;
			}

			if (Height < 12 * 4) {
				Height = 12 * 4;
			}

			PositionChildren();
			base.OnBoundsChangedDirect();
		}


		/// <inheritdoc />
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			tools.Background.Rectangle();
			tools.Background.BevelBorder();

			if (m_useInnerBorder) {
				var innerBorderBounds = m_body.Bounds;
				innerBorderBounds.Inflate(-m_innerBorderSize, -m_innerBorderSize);
				tools.Background.BevelBorder(innerBorderBounds, true);

				if (m_useInnerBorderShadow) {
					tools.InnerGlow(innerBorderBounds);
				}
			}

			tools.Shine(drawParameters.Bounds, true);
		}

		/// <summary>
		/// Positions children correctly
		/// </summary>
		private void PositionChildren() {
			m_header.X = 0;
			m_header.Y = 0;
			m_header.Width = Width;
			m_header.Height = m_headerHeight;
			m_body.X = 0;
			m_body.Top = m_header.Bottom;
			m_body.Height = Height - m_header.Height;
			m_body.Width = Width;
		}

		/// <summary>
		/// Closes the window. Disposes if DisposeOnClose is set to true
		/// </summary>
		public void Close() {
			Visible = false;
			OnClosing();

			if (DisposeOnClose) {
				Dispose();
			}
		}

		/// <summary>
		/// Opens the window.
		/// </summary>
		public void Open() {
			Visible = true;
			Enabled = true;
			OnOpening();
			MoveToFront();
		}

		/// <summary>
		/// Opens or closes window depending on if its visible
		/// </summary>
		public void Toggle() {
			if (Visible) {
				Close();
			} else {
				Open();
			}
		}

		/// <inheritdoc />
		protected override void OnContainFocusGained() {
			MoveToFront();
			base.OnContainFocusGained();
		}

		/// <summary>
		/// Invoked when the window is closing
		/// </summary>
		protected virtual void OnClosing() => Closing?.Invoke();

		/// <summary>
		/// Invoked when the window is opening
		/// </summary>
		protected virtual void OnOpening() => Opening?.Invoke();
	}
}