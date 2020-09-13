using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectInput;
using SharpDX.DirectWrite;

#pragma warning disable 67

namespace DXToolKit.GUI {
	/// <summary>
	/// Base element for everything GUI
	/// </summary>
	public abstract partial class GUIElement : IDisposable {
		#region Static

		/// <summary>
		/// Gets or sets a value indicating the total amount of redraw counts processed by all GUI Elements
		/// </summary>
		public static long RedrawCount = 0;

		/// <summary>
		/// Invoked every time a element is redrawn, use full for debugging
		/// </summary>
		public static event Action<GUIElement> ElementRedraw;

		/// <summary>
		/// Gets the base element of the current gui system.
		/// This will not be set before EnableGUI() is called.
		/// </summary>
		public static GUIElement BaseElement { get; internal set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a new GUIElement
		/// </summary>
		/// <exception cref="Exception">GUISystem must be created before any GUIElements can be created</exception>
		protected GUIElement() {
			if (BaseElement == null) {
				throw new Exception("Cannot create GUI Elements before enabeling GUI. Create a GUISystem or run Scene CreateGUI() before starting to create GUIElements");
			}
		}

		/// <summary>
		/// Bypass for creating the base element
		/// </summary>
		internal GUIElement(bool baseElement) {
			if (baseElement) {
				BaseElement = this;
			}
		}

		#endregion

		#region Fields

		/// <summary>
		/// Gets or sets a value indicating if the element can receive mouse input
		/// </summary>
		public bool CanReceiveMouseInput = true;

		/// <summary>
		/// If the control can receive keyboard input
		/// </summary>
		public bool CanReceiveKeyboardInput = true;

		/// <summary>
		/// If the control can capture keyboard input.
		/// CanReceiveKeyboardInput can still be true, but this can be set to false if you want to allow the keypress to propagate through to the game layer 
		/// </summary>
		public bool CanCaptureKeyboardInput = true;

		/// <summary>
		/// Gets or sets a value indicating if this element can be dragged
		/// </summary>
		public bool Draggable = false;

		/// <summary>
		/// Gets or sets a value indicating if this control can receive focus
		/// </summary>
		public bool CanReceiveFocus = true;

		/// <summary>
		/// Calculated screen bounds based on current elements local position added with parents screen position
		/// </summary>
		private RectangleF m_screenBounds;

		/// <summary>
		/// Single variable controlling the bounds of the element.
		/// Every change to location or size should modify this.
		/// </summary>
		private RectangleF m_bounds = new RectangleF(0, 0, 100, 100);

		/// <summary>
		/// Controller to limit the amount of "bounds updated" events generated each frame
		/// </summary>
		private bool m_hasBoundsChanged;

		/// <summary>
		/// Controller to limit the amount of "size changed" events generated each frame
		/// </summary>
		private bool m_hasSizeChanged;

		/// <summary>
		/// Controller to limit the amount of "location changed" events generated each frame
		/// </summary>
		private bool m_hasLocationChanged;

		/// <summary>
		/// A queue of actions to move child elements to front/back depending on action. This is done so we cant move an element to the front of m_childElements while we are inside a update of one of those child elements
		/// </summary>
		private Queue<Action> m_moveActions = new Queue<Action>();

		/// <summary>
		/// Controller to toggle if the element gets updated and rendered
		/// </summary>
		private bool m_enabled = true;

		/// <summary>
		/// Controller for if this element has focus
		/// </summary>
		private bool m_isFocused;

		/// <summary>
		/// Controller to set focus to this element the next update
		/// </summary>
		private bool m_isNewFocusTarget;

		/// <summary>
		/// Controller for if this element or any of its children has focus
		/// </summary>
		private bool m_containsFocus;

		/// <summary>
		/// Controller for if this control contains the mouse in screen coordinates
		/// </summary>
		private bool m_containsMouse;

		/// <summary>
		/// Controller to keep track of if the mouse is pressed while over the control
		/// </summary>
		private bool m_isMousePressed;

		/// <summary>
		/// Controller for if OnMouseWheel events should be fired even if this control does not have focus
		/// </summary>
		private bool m_mouseWheelNeedsFocus = false;

		/// <summary>
		/// Parent element, will be NULL by default, and NULL if its the root element
		/// </summary>
		private GUIElement m_parentElement;

		/// <summary>
		/// List of child elements this element contains
		/// </summary>
		private List<GUIElement> m_childElements = new List<GUIElement>();

		/// <summary>
		/// Render texture used to draw the gui element
		/// </summary>
		private GUIRenderTexture m_renderTexture;

		/// <summary>
		/// Controller to handle only resizing rendertexture once
		/// </summary>
		private bool m_resizeRenderTexture;

		/// <summary>
		/// Controller to trigger a redraw of the gui element
		/// </summary>
		private bool m_redraw = true;

		/// <summary>
		/// Controller (probably not needed) to control if the element is visible
		/// </summary>
		private bool m_visible = true;

		/// <summary>
		/// Controller for the bitmap opacity when rendered onto parent
		/// </summary>
		private float m_opacity = 1.0F;

		/// <summary>
		/// Render offset used to offset child elements position when rendering. Useful for things like text boxes, list boxes with scrollbars, etc
		/// </summary>
		private Vector2 m_renderOffset = Vector2.Zero;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the parent element of this element
		/// This is read only. Use newParent.append(this) to change parent
		/// </summary>
		public GUIElement Parent => m_parentElement;

		/// <summary>
		/// Gets the root gui element
		/// </summary>
		public GUIElement Root {
			get {
				var target = Parent;

				// Keep recursing until target.parent is null
				while (target != null) {
					// if target parent is null, return the target, since we've reached the root
					if (target.Parent == null) {
						return target;
					}

					// Keep recursing
					target = target.Parent;
				}

				// Case if we're already on the root node
				return this;
			}
		}

		/// <summary>
		/// Gets the child elements of this element
		/// This is read only, use Append or Remove to add/remove child elements
		/// </summary>
		public List<GUIElement> ChildElements => m_childElements;

		/// <summary>
		/// Gets a value indicating if the element is currently being dragged
		/// </summary>
		public bool IsDragged { get; private set; }

		/// <summary>
		/// Gets a value indicating if the mouse is hovering over the element
		/// </summary>
		public bool MouseHovering { get; private set; }

		/// <summary>
		/// Gets a value indicating if the element is focused
		/// </summary>
		public bool Focused => m_isFocused;

		/// <summary>
		/// Gets a value indicating if the element contains focus, meaning if this element or any of its children has focus
		/// </summary>
		public bool ContainsFocus => m_containsFocus;

		/// <summary>
		/// Gets or sets the local bounds of the element
		/// </summary>
		/// <remarks>Be aware that changing this will result in a rescaling of the base texture, which can be slow depending on the size of the control</remarks>
		/// <exception cref="ArgumentOutOfRangeException">Will throw an exception if width or height is less then 1</exception>
		public RectangleF Bounds {
			// Direct return the bounds
			get => m_bounds;
			set {
				// Constrain size
				if (value.Width < MinimumWidth) value.Width = MinimumWidth;
				if (value.Height < MinimumHeight) value.Width = MinimumHeight;

				// Check width and height
				if (value.Width < 1) throw new ArgumentOutOfRangeException(nameof(value.Width), value.Width, "Cannot be less then 1");
				if (value.Height < 1) throw new ArgumentOutOfRangeException(nameof(value.Height), value.Height, "Cannot be less then 1");

				// Do a rectangle comparison to check if anything has changed, if so assign and set every controller boolean to true
				if (value != m_bounds) {
					m_bounds = value;
					m_hasBoundsChanged = true;
					m_hasSizeChanged = true;
					m_hasLocationChanged = true;
					HandleConstrained();
					OnBoundsChangedDirect();
				}
			}
		}

		/// <summary>
		/// The X coordinate of the elements left side
		/// </summary>
		public float X {
			get => m_bounds.X;
			set {
				if (!MathUtil.NearEqual(value, m_bounds.X)) {
					m_hasLocationChanged = true;
					m_hasBoundsChanged = true;
					m_bounds.X = value;
					HandleConstrained();
					OnBoundsChangedDirect();
				}
			}
		}

		/// <summary>
		/// The Y coordinate of the elements top side
		/// </summary>
		public float Y {
			get => m_bounds.Y;
			set {
				if (!MathUtil.NearEqual(value, m_bounds.Y)) {
					m_hasLocationChanged = true;
					m_hasBoundsChanged = true;
					m_bounds.Y = value;
					HandleConstrained();
					OnBoundsChangedDirect();
				}
			}
		}


		/// <summary>
		/// Gets or sets the width of the element in pixels
		/// </summary>
		/// <remarks>Be aware that changing this will result in a rescaling of the base texture, which can be slow depending on the size of the control</remarks>
		/// <exception cref="ArgumentOutOfRangeException">Throws an exception if width is less then 1</exception>
		public float Width {
			get => m_bounds.Width;
			set {
				if (value < MinimumWidth) value = MinimumWidth;
				if (value < 1) throw new ArgumentOutOfRangeException(nameof(Width), value, "Cannot be less then 1");
				if (!MathUtil.NearEqual(value, m_bounds.Width)) {
					m_hasBoundsChanged = true;
					m_hasSizeChanged = true;
					m_bounds.Width = value;
					HandleConstrained();
					OnBoundsChangedDirect();
				}
			}
		}

		/// <summary>
		/// Gets or sets the height of the element in pixels
		/// </summary>
		/// <remarks>Be aware that changing this will result in a rescaling of the base texture, which can be slow depending on the size of the control</remarks>
		/// <exception cref="ArgumentOutOfRangeException">Throws an exception if the height is less then 1</exception>
		public float Height {
			get => m_bounds.Height;
			set {
				if (value < MinimumHeight) value = MinimumHeight;
				if (value < 1) throw new ArgumentOutOfRangeException(nameof(Height), value, "Cannot be less then 1");
				if (!MathUtil.NearEqual(value, m_bounds.Height)) {
					m_bounds.Height = value;
					m_hasBoundsChanged = true;
					m_hasSizeChanged = true;
					HandleConstrained();
					OnBoundsChangedDirect();
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating if this element should be constrained to parents bounds.
		/// Good for things like windows, popups etc that are directly assigned to the base element.
		/// </summary>
		public bool ConstrainToParent = false;

		/// <summary>
		/// Handles constrained to parents bounds
		/// </summary>
		private void HandleConstrained() {
			if (ConstrainToParent && m_parentElement != null) {
				if (m_bounds.X < 0) {
					m_bounds.X = 0;
				}

				if (m_bounds.Y < 0) {
					m_bounds.Y = 0;
				}

				if (m_bounds.Width > m_parentElement.Width) {
					throw new Exception("Cannot constrain a child element to parents bounds if the child element is wider then its parent");
				}

				if (m_bounds.Height > m_parentElement.Height) {
					throw new Exception("Cannot constrain a child element to parents bounds if the child element is taller then its parent");
				}

				if (m_bounds.X + m_bounds.Width > m_parentElement.Width) {
					m_bounds.X = m_parentElement.Width - m_bounds.Width;
				}

				if (m_bounds.Y + m_bounds.Height > m_parentElement.Height) {
					m_bounds.Y = m_parentElement.Height - m_bounds.Height;
				}
			}
		}

		/// <summary>
		/// Override to set a custom minimum width of the element
		/// </summary>
		protected virtual float MinimumWidth { get; } = 1.0F;

		/// <summary>
		/// Override to set a custom minimum height of the element
		/// </summary>
		protected virtual float MinimumHeight { get; } = 1.0f;

		/// <summary>
		/// Gets the screen bounds of the gui element
		/// </summary>
		public RectangleF ScreenBounds {
			get {
				// Copy width and height from local bounds
				m_screenBounds.Width = m_bounds.Width;
				m_screenBounds.Height = m_bounds.Height;

				// Check if we have a parent, add its screen bounds to ours
				if (m_parentElement != null) {
					m_screenBounds.Location = m_bounds.Location + m_parentElement.ScreenBounds.Location + m_parentElement.m_renderOffset;
					return m_screenBounds;
				}

				// Default to local x and y
				m_screenBounds.X = m_bounds.X;
				m_screenBounds.Y = m_bounds.Y;
				return m_screenBounds;
			}
		}

		/// <summary>
		/// Gets or sets a value if the element is enabled. If disabled, element wont receive update calls or render calls
		/// To just hide the element, use Visible prop, so that element is still updated
		/// </summary>
		public bool Enabled {
			get => m_enabled;
			set {
				if (m_enabled != value) {
					// Even if new value is false, a redraw toggle is needed to "not draw" the element onto its parent
					ToggleRedraw();
					// Invoke events
					OnEnabledChanged(value);
				}

				m_enabled = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating if the element should be rendered.
		/// </summary>
		public bool Visible {
			get => m_visible;
			set {
				if (m_visible != value) {
					// Even if new value is false, a redraw toggle is needed to "not draw" the element onto its parent
					ToggleRedraw();
					// Invoke events
					OnVisibilityChanged(value);
				}

				m_visible = value;
			}
		}


		/// <summary>
		/// Gets or sets the opacity of this element when its rendered onto its parent.
		/// This will affect all child elements aswell
		/// </summary>
		public float Opacity {
			get => m_opacity;
			set {
				if (MathUtil.NearEqual(value, m_opacity) == false) {
					m_opacity = value;
					ToggleRedraw();
				}
			}
		}


		/// <summary>
		/// Gets or sets the render offset of the gui element.
		/// Render offset controls where child elements are drawn.
		/// This also correctly calculates screen position.
		/// </summary>
		public Vector2 RenderOffset {
			get => m_renderOffset;
			set {
				if (m_renderOffset != value) {
					ToggleRedraw();
					m_renderOffset = value;
				}
			}
		}

		/// <summary>
		/// Defaults to return this.ScreenBounds, but can be adjusted as desired
		/// </summary>
		protected virtual RectangleF MouseScreenBounds => ScreenBounds;

		/// <summary>
		/// Gets or sets the left of the control
		/// </summary>
		public float Left {
			get => X;
			set => X = value;
		}

		/// <summary>
		/// Gets or sets the top of the control
		/// </summary>
		public float Top {
			get => Y;
			set => Y = value;
		}

		/// <summary>
		/// Gets or sets the right of the control (this does not rescale the control)
		/// </summary>
		public float Right {
			get => X + Width;
			set => X = value - Width;
		}

		/// <summary>
		/// Gets or sets the bottom of the control (this does not rescale the control)
		/// </summary>
		public float Bottom {
			get => Y + Height;
			set => Y = value - Height;
		}

		/// <summary>
		/// Gets or sets the center of the element
		/// </summary>
		public Vector2 Center {
			get => m_bounds.Center;
			set {
				X = value.X - Width / 2.0F;
				Y = value.Y - Height / 2.0F;
			}
		}

		/// <summary>
		/// Gets or sets the center X position of the element
		/// </summary>
		public float CenterX {
			get => m_bounds.Center.X;
			set => X = value - Width / 2.0F;
		}

		/// <summary>
		/// Gets or sets the center Y position of the element
		/// </summary>
		public float CenterY {
			get => m_bounds.Center.Y;
			set => Y = value - Height / 2.0F;
		}

		/// <summary>
		/// Gets or sets the location of the element
		/// </summary>
		public Vector2 Location {
			get => m_bounds.Location;
			set {
				X = value.X;
				Y = value.Y;
			}
		}

		/// <summary>
		/// Gets or sets the top left position of the element
		/// </summary>
		public Vector2 TopLeft {
			get => m_bounds.TopLeft;
			set {
				Top = value.Y;
				Left = value.X;
			}
		}

		/// <summary>
		/// Gets or sets the top right position of the element
		/// </summary>
		public Vector2 TopRight {
			get => m_bounds.TopRight;
			set {
				Top = value.Y;
				Right = value.X;
			}
		}

		/// <summary>
		/// Gets or sets the bottom left position of the element
		/// </summary>
		public Vector2 BottomLeft {
			get => m_bounds.BottomLeft;
			set {
				Bottom = value.Y;
				Left = value.X;
			}
		}

		/// <summary>
		/// Gets or sets the bottom right position of the element
		/// </summary>
		public Vector2 BottomRight {
			get => m_bounds.BottomRight;
			set {
				Bottom = value.Y;
				Right = value.X;
			}
		}


		/// <summary>
		/// Gets a value indicating if this control contains the mouse
		/// </summary>
		public bool ContainsMouse => m_containsMouse;

		/// <summary>
		/// Gets a value indicating if the mouse is being pressed while hovering over the elements bounds
		/// </summary>
		public bool IsMousePressed => m_isMousePressed;

		/// <summary>
		/// Gets or sets a value indicating if OnMouseWheel events should be raised even if the control does not contain focus
		/// Default: false
		/// </summary>
		public bool MouseWheelNeedsFocus {
			get => m_mouseWheelNeedsFocus;
			set => m_mouseWheelNeedsFocus = value;
		}

		/// <summary>
		/// Sets a custom transform on the rendertarget. Be aware that this does not affect things like mouse bounds checks
		/// </summary>
		public Matrix3x2 RenderTransform = Matrix3x2.Identity;

		/// <summary>
		/// Default to return Bounds, but can be adjusted to clip child contents to a given value
		/// <remarks>UseClippingBounds must return true for rendering pipeline to use this. Reason being, im not 100% sure about the logic behind clipping, and thus its disabled by default</remarks>
		/// </summary>
		protected virtual RectangleF ClippedRenderBounds => m_bounds;

		/// <summary>
		/// Override and return true to use ClippedRenderBounds
		/// </summary>
		protected virtual bool UseClippingBounds => false;

		/// <summary>
		/// Gets or sets the tool tip text. If NULL this element wont trigger a tool tip popup
		/// </summary>
		public string Tooltip = null;

		#endregion

		#region Events

		/// <summary>
		/// Invoked when enabled property is changed
		/// </summary>
		public event Action<bool> EnabledChanged;

		/// <summary>
		/// Invoked when visible property is changed
		/// </summary>
		public event Action<bool> VisibilityChanged;

		/// <summary>
		/// Raised if elements size has changed the last frame
		/// </summary>
		public event Action Resized;

		/// <summary>
		/// Raised if element bounds has changed the last frame
		/// </summary>
		public event Action BoundsChanged;

		/// <summary>
		/// Raised directly after bounds has been updated, instead of raised maximum once each frame
		/// </summary>
		public event Action BoundsChangedDirect;

		/// <summary>
		/// Raised if elements location has changed the last frame
		/// </summary>
		public event Action LocationChanged;

		/// <summary>
		/// Raised when the element looses focus
		/// </summary>
		public event Action FocusLost;

		/// <summary>
		/// Raised when the element gains focus
		/// </summary>
		public event Action FocusGained;

		/// <summary>
		/// Raised when the mouse enters the bounds of the element
		/// </summary>
		public event Action MouseEnter;

		/// <summary>
		/// Raised when the mouse leaves the bounds of the element
		/// </summary>
		public event Action MouseLeave;

		/// <summary>
		/// Raised each frame when the mouse is hovering over the element
		/// </summary>
		public event Action MouseHover;

		/// <summary>
		/// Raised when the element is starting to get dragged
		/// </summary>
		public event Action DragStart;

		/// <summary>
		/// Raised each frame when the element is being dragged
		/// </summary>
		public event Action Drag;

		/// <summary>
		/// Raised when the element stops being dragged
		/// </summary>
		public event Action DragStop;

		/// <summary>
		/// Raised when the element runs a redraw
		/// </summary>
		public event Action OnRedraw;

		/// <summary>
		/// Raised when a mouse button is pressed down while the mouse is hovering over the element
		/// </summary>
		public event Action<GUIMouseEventArgs> MouseDown;

		/// <summary>
		/// Raised when a mouse button is released while the mouse is hovering over the element
		/// </summary>
		public event Action<GUIMouseEventArgs> MouseUp;

		/// <summary>
		/// Raised when a mouse button is held while the mouse is hovering over the element
		/// </summary>
		public event Action<GUIMouseEventArgs> MousePressed;

		/// <summary>
		/// Raised when the left mouse button is released while hovering over the element
		/// </summary>
		public event Action<GUIMouseEventArgs> Click;

		/// <summary>
		/// Raised when a mouse button is double clicked while hovering over the element
		/// </summary>
		public event Action<GUIMouseEventArgs> DoubleClick;

		/// <summary>
		/// Raised if the mousewheel is scrolled while mouse is hovering over this control
		/// </summary>
		public event Action<float> MosueWheel;

		/// <summary>
		/// Raised when a key is pressed down this frame
		/// </summary>
		public event Action<List<Key>> KeyDown;

		/// <summary>
		/// Raised when a key is released this frame
		/// </summary>
		public event Action<List<Key>> KeyUp;

		/// <summary>
		/// Raised when a key is held
		/// </summary>
		public event Action<List<Key>> KeyPressed;

		/// <summary>
		/// Raised on text input with a string containing the current text buffer for this frame
		/// </summary>
		public event Action<string> TextInput;

		/// <summary>
		/// Raised on the system repeating input interval
		/// </summary>
		public event Action<Key> RepeatKey;

		/// <summary>
		/// Raised if text has changed since the last frame
		/// </summary>
		public event Action<string> TextChanged;

		/// <summary>
		/// Invoked if properties of the text has changed (Font, size, alignment, etc)
		/// </summary>
		public event Action TextPropertiesChanged;

		/// <summary>
		/// Raised when this or a child receives focus
		/// </summary>
		public event Action ContainFocusGained;

		/// <summary>
		/// Raised when this or a child looses focus
		/// </summary>
		public event Action ContainFocusLost;

		/// <summary>
		/// Raised each time the element is updated (usually once per frame)
		/// </summary>
		public event Action Updated;

		/// <summary>
		/// Delegate used for when parent changes
		/// </summary>
		/// <param name="newParent">The newly set parent</param>
		/// <param name="oldParent">The previous parent (can be NULL)</param>
		public delegate void ParentChangeHandler(GUIElement newParent, GUIElement oldParent);

		/// <summary>
		/// Delegate used for when a element is appended or removed
		/// </summary>
		/// <param name="parent">The current/previous parent</param>
		/// <param name="child">The target element that was added</param>
		public delegate void AppendHandler(GUIElement parent, GUIElement child);

		/// <summary>
		/// Raised when parent element changes
		/// </summary>
		public event ParentChangeHandler ParentChanged;

		/// <summary>
		/// Raised when a new child is appended to this element
		/// </summary>
		public event AppendHandler ChildAppended;

		/// <summary>
		/// Raised when a child element is removed from this element
		/// </summary>
		public event AppendHandler ChildRemoved;

		/// <summary>
		/// Raised when this elements parent is set
		/// </summary>
		public event AppendHandler ParentSet;

		/// <summary>
		/// Raised when this elements parent is unset
		/// </summary>
		public event AppendHandler ParentUnset;

		/// <summary>
		/// Invoked when enabled property is changed
		/// </summary>
		protected virtual void OnEnabledChanged(bool value) => EnabledChanged?.Invoke(value);

		/// <summary>
		/// Invoked when visible property is changed
		/// </summary>
		protected virtual void OnVisibilityChanged(bool value) => VisibilityChanged?.Invoke(value);

		/// <summary>
		/// Invoked on LateUpdate at a maximum of once per frame after the Elements bounds has changed.
		/// For a more direct callback, check OnBoundsChangedDirect
		/// Note: Will only be invoked if input value differs from stored value.
		/// </summary>
		protected virtual void OnBoundsChanged() => BoundsChanged?.Invoke();

		/// <summary>
		/// Invoked immediately after any bounds has been changed.
		/// Note: Will only be invoked if input value differs from stored value.
		/// </summary>
		protected virtual void OnBoundsChangedDirect() => BoundsChangedDirect?.Invoke();

		/// <summary>
		/// Invoked on LateUpdate at a maximum of once per frame if location has changed
		/// Note: Will only be invoked if input value differs from stored value.
		/// </summary>
		protected virtual void OnLocationChanged() => LocationChanged?.Invoke();

		/// <summary>
		/// Invoked on LateUpdate at a maximum of once per frame if size has changed
		/// Note: Will only be invoked if input value differs from stored value.
		/// </summary>
		protected virtual void OnResize() => Resized?.Invoke();

		/// <summary>
		/// Invoked on PreUpdate if focus was lost
		/// </summary>
		protected virtual void OnFocusLost() => FocusLost?.Invoke();

		/// <summary>
		/// Invoked on PreUpdate if focus was gained
		/// </summary>
		protected virtual void OnFocusGained() => FocusGained?.Invoke();

		/// <summary>
		/// Invoked if the mouse enters the controls bounds
		/// </summary>
		protected virtual void OnMouseEnter() => MouseEnter?.Invoke();

		/// <summary>
		/// Invoked if the mouse leaves the controls bounds
		/// </summary>
		protected virtual void OnMouseLeave() => MouseLeave?.Invoke();

		/// <summary>
		/// Invoked every frame the mouse is hovering above the controls bounds
		/// </summary>
		protected virtual void OnMouseHover() => MouseHover?.Invoke();

		/// <summary>
		/// Invoked when a drag operation is started.
		/// Node: Draggable must be set to TRUE to allow for this to fire
		/// </summary>
		protected virtual void OnDragStart() => DragStart?.Invoke();

		/// <summary>
		/// Invoked every frame the element is dragged
		/// Node: Draggable must be set to TRUE to allow for this to fire
		/// </summary>
		protected virtual void OnDrag() => Drag?.Invoke();

		/// <summary>
		/// Invoked when the element is "dropped" after a drag
		/// Node: Draggable must be set to TRUE to allow for this to fire
		/// </summary>
		protected virtual void OnDragStop() => DragStop?.Invoke();

		/// <summary>
		/// Invoked if the mouse is over the control and a mouse button is down this frame
		/// </summary>
		/// <param name="args">MouseEventArgs</param>
		protected virtual void OnMouseDown(GUIMouseEventArgs args) => MouseDown?.Invoke(args);

		/// <summary>
		/// Invoked if the mouse is over the control and a mouse button is up this frame
		/// </summary>
		/// <param name="args">MouseEventArgs</param>
		protected virtual void OnMouseUp(GUIMouseEventArgs args) => MouseUp?.Invoke(args);

		/// <summary>
		/// Invoked if the mouse is over the control and a mouse button is pressed this frame
		/// </summary>
		/// <param name="args">MouseEventArgs</param>
		protected virtual void OnMousePressed(GUIMouseEventArgs args) => MousePressed?.Invoke(args);

		/// <summary>
		/// Invoked if the mouse is over the control and a mouse button is up this frame ( same as OnMouseUp )
		/// </summary>
		/// <param name="args">MouseEventArgs</param>
		protected virtual void OnClick(GUIMouseEventArgs args) => Click?.Invoke(args);

		/// <summary>
		/// Invoked if the mouse is over the control and a double click is registered this frame
		/// </summary>
		/// <param name="args">MouseEventArgs</param>
		protected virtual void OnDoubleClick(GUIMouseEventArgs args) => DoubleClick?.Invoke(args);

		/// <summary>
		/// Invoked if the mousewheel has moved this frame.
		/// Use MouseWheelNeedsFocus to toggle if this should be invoked even if element is not in focus
		/// </summary>
		/// <param name="delta">The mouse wheel movement this frame</param>
		protected virtual void OnMouseWheel(float delta) => MosueWheel?.Invoke(delta);


		/// <summary>
		/// Invoked if this element has focus and a key was pressed this frame
		/// </summary>
		/// <param name="keys">The pressed keys</param>
		protected virtual void OnKeyDown(List<Key> keys) => KeyDown?.Invoke(keys);

		/// <summary>
		/// Invoked if this element has focus and a key was released this frame
		/// </summary>
		/// <param name="keys">The pressed keys</param>
		protected virtual void OnKeyUp(List<Key> keys) => KeyUp?.Invoke(keys);

		/// <summary>
		/// Invoked if this element has focus and a key is pressed this frame
		/// </summary>
		/// <param name="keys">The pressed keys</param>
		protected virtual void OnKeyPressed(List<Key> keys) => KeyPressed?.Invoke(keys);

		/// <summary>
		/// Invoked if this element has focus and text input was received this frame (Normal input with repeats etc)
		/// </summary>
		/// <param name="text">Input formatted as a string to handle more charecters</param>
		protected virtual void OnTextInput(string text) => TextInput?.Invoke(text);

		/// <summary>
		/// Invoked if this element has focus and a key is being held with the windows repeating timer
		/// </summary>
		/// <param name="key">The repeating keys</param>
		protected virtual void OnRepeatKey(Key key) => RepeatKey?.Invoke(key);

		/// <summary>
		/// Invoked if the stored Text property has changed
		/// </summary>
		/// <param name="text">The new text</param>
		protected virtual void OnTextChanged(string text) => TextChanged?.Invoke(text);

		/// <summary>
		/// Invoked if this element or any of its child elements lost focus
		/// </summary>
		protected virtual void OnContainFocusGained() => ContainFocusGained?.Invoke();

		/// <summary>
		/// Invoked if this element or any of its child elements gained focus
		/// </summary>
		protected virtual void OnContainFocusLost() => ContainFocusLost?.Invoke();

		/// <summary>
		/// Invoked if parent changed
		/// </summary>
		/// <param name="newParent"></param>
		/// <param name="oldParent"></param>
		protected virtual void OnParentChanged(GUIElement newParent, GUIElement oldParent) => ParentChanged?.Invoke(newParent, oldParent);

		/// <summary>
		/// Invoked when a child is appended to this control
		/// </summary>
		/// <param name="parent">The parent control (usually this)</param>
		/// <param name="child">The child control</param>
		protected virtual void OnChildAppended(GUIElement parent, GUIElement child) => ChildAppended?.Invoke(parent, child);

		/// <summary>
		/// Invoked when a child was removed from this control
		/// </summary>
		/// <param name="parent">The parent control (usually this)</param>
		/// <param name="child">The child control</param>
		protected virtual void OnChildRemoved(GUIElement parent, GUIElement child) => ChildRemoved?.Invoke(parent, child);

		/// <summary>
		/// Invoked when this elements parent was set
		/// </summary>
		/// <param name="parent">The new parent element</param>
		/// <param name="child">The child (usually this)</param>
		protected virtual void OnParentSet(GUIElement parent, GUIElement child) => ParentSet?.Invoke(parent, child);

		/// <summary>
		/// Invoked when this elements parent was unset
		/// </summary>
		/// <param name="parent">The parent element that was set previously</param>
		/// <param name="child">The child (usually this)</param>
		protected virtual void OnParentUnset(GUIElement parent, GUIElement child) => ParentUnset?.Invoke(parent, child);

		/// <summary>
		/// Invoked if properties of the text has changed (Font, size, alignment, etc)
		/// </summary>
		protected virtual void OnTextPropertiesChanged() => TextPropertiesChanged?.Invoke();

		#endregion

		#region Virtuals

		/// <summary>
		/// Runs before OnUpdate
		/// </summary>
		protected virtual void OnPreUpdate() { }

		/// <summary>
		/// Runs once every frame, if this element is Enabled
		/// </summary>
		protected virtual void OnUpdate() => Updated?.Invoke();

		/// <summary>
		/// Runs after update
		/// </summary>
		protected virtual void OnLateUpdate() { }

		/// <summary>
		/// Runs when this element needs a redraw
		/// </summary>
		/// <param name="renderTarget">Rendertarget to use when rendering the element</param>
		/// <param name="bounds">The bounds the element should render into</param>
		/// <param name="textLayout">Text layout for text rendering</param>
		protected virtual void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout) { }

		/// <summary>
		/// Runs after child elements have been rendered
		/// </summary>
		/// <param name="renderTarget">Rendertarget to use when rendering the element</param>
		/// <param name="bounds">The bounds the element should render into</param>
		/// <param name="textLayout">Text layout for text rendering</param>
		protected virtual void PostRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout) { }

		/// <summary>
		/// Virtual to add mor functionality to dispose
		/// </summary>
		protected virtual void OnDispose() { }

		#endregion

		#region Dispose

		/// <summary>
		/// Gets a value indicating if this element is disposed
		/// </summary>
		public bool IsDisposed { get; private set; } = false;

		/// <summary>
		/// Gets a value indicating if this element is being disposed of.
		/// </summary>
		public bool IsDisposing { get; private set; } = false;

		/// <inheritdoc />
		public void Dispose() {
			// Set disposing to true
			IsDisposing = true;
			// Remove from parent if set, with explicit false on dispose, since were already here
			m_parentElement?.Remove(this, false);
			// Set parent to null, should already be null when we get here
			m_parentElement = null;
			// Disable
			m_enabled = false;
			m_visible = false;
			// Dispose of disposables
			Utilities.Dispose(ref m_guiText);
			Utilities.Dispose(ref m_renderTexture);
			// Dispose and remove child elements
			if (m_childElements != null) {
				while (m_childElements.Count > 0) {
					// Child element dispose will remove it from the m_childElements
					m_childElements[0]?.Dispose();
				}
			}

			// Call for event handlers
			OnDispose();
			// Set is disposed
			IsDisposed = true;
		}

		#endregion

		#region Helper Functions

		/// <summary>
		/// Moves the control by the input amount
		/// </summary>
		/// <param name="amount">Amount as a Vector2</param>
		public void Move(Vector2 amount) {
			X += amount.X;
			Y += amount.Y;
		}

		/// <summary>
		/// Moves the control by the input amount
		/// </summary>
		/// <param name="x">Amount of pixels to move in the X direction</param>
		/// <param name="y">Amount of pixels to move in the Y direction</param>
		public void Move(float x, float y) {
			X += x;
			Y += y;
		}

		/// <summary>
		/// Gets the first child of a given type
		/// NULL if none are found
		/// </summary>
		/// <typeparam name="T">The type to look for</typeparam>
		/// <returns>First GUIElement of type T or NULL</returns>
		public T GetFirstChildOfType<T>(bool recurse = true) where T : GUIElement {
			if (m_childElements != null) {
				if (recurse) {
					foreach (var el in AllChildren()) {
						if (el is T target) {
							return target;
						}
					}
				} else {
					foreach (var el in m_childElements) {
						if (el is T target) {
							return target;
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Returns all children of a given type
		/// </summary>
		/// <param name="recurse">If search should recurse down the hierarchy</param>
		/// <typeparam name="T">The GUIElement type to look for</typeparam>
		/// <returns>IEnumerable of GUIElements that matches the specified type</returns>
		public IEnumerable<T> GetChildrenOfType<T>(bool recurse = false) where T : GUIElement {
			if (m_childElements != null) {
				foreach (var el in m_childElements) {
					if (recurse) {
						foreach (var childEl in el.GetChildrenOfType<T>(true)) {
							yield return childEl;
						}
					}

					if (el is T target) {
						yield return target;
					}
				}
			}
		}

		/// <summary>
		/// Copies the text parameters from another element
		/// </summary>
		/// <param name="other">The other element to copy parameters from</param>
		public void CopyTextParameters(GUIElement other) {
			WordWrapping = other.WordWrapping;
			TextAlignment = other.TextAlignment;
			ParagraphAlignment = other.ParagraphAlignment;
			Font = other.Font;
			FontSize = other.FontSize;
			FontWeight = other.FontWeight;
			FontStretch = other.FontStretch;
			FontStyle = other.FontStyle;
		}

		#endregion
	}
}