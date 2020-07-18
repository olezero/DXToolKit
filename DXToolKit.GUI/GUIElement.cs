using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectInput;
using SharpDX.DirectWrite;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;
using TextAlignment = SharpDX.DirectWrite.TextAlignment;

namespace DXToolKit.GUI {
	public abstract partial class GUIElement : IDisposable {
		// TODO - FIX OnContainedFocusLost ( seams to be called even if a child element is focused )

		// TODO - Need a layering system, for instance to allow windows to always be drawn on top of everything else. Like a z-index
		// TODO - Variable for "move to front on focus gained", to allow for toggling that functionality


		#region Fields

		/// <summary>
		/// Gets or sets the default font used by the GUI, this is a global variable
		/// </summary>
		public static string DEFAULT_FONT = "Arial";

		/// <summary>
		/// Gets or sets the default font size used by the GUI, this is a global variable
		/// </summary>
		public static int DEFAULT_FONT_SIZE = 14;

		/// <summary>
		/// Stored text in the element
		/// </summary>
		private string m_text = "";

		/// <summary>
		/// Container for the gui text used for rendering
		/// </summary>
		private GUIText m_guiText = new GUIText(DEFAULT_FONT, DEFAULT_FONT_SIZE);

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
		/// Controller to run text change events
		/// </summary>
		private bool m_hasTextChanged;

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
		/// A queue of actions to move child elements to front/back depending on action. This is done so we cant move an element to the front of m_childElements while we are inside a update of one of those child elements
		/// </summary>
		private Queue<Action> m_moveActions = new Queue<Action>();

		/// <summary>
		/// Controller to handle only resizing rendertexture once
		/// </summary>
		private bool m_resizeRenderTexture;

		/// <summary>
		/// Controller to trigger a redraw of the gui element
		/// </summary>
		private bool m_redraw = true;

		/// <summary>
		/// Controller to toggle if the element gets updated and rendered
		/// </summary>
		private bool m_enabled = true;

		/// <summary>
		/// Controller (probably not needed) to control if the element is visible
		/// </summary>
		private bool m_visible = true;

		/// <summary>
		/// Render offset used to offset child elements position when rendering. Useful for things like text boxes, list boxes with scrollbars, etc
		/// </summary>
		private Vector2 m_renderOffset = Vector2.Zero;

		/// <summary>
		/// Controller for if this element has focus
		/// </summary>
		private bool m_isFocused;

		/// <summary>
		/// Controller for if this element or any of its children has focus
		/// </summary>
		private bool m_containsFocus;

		/// <summary>
		/// Controller for if this control contains the mouse in screen coordinates
		/// </summary>
		private bool m_containsMouse;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the parent element of this element
		/// This is read only. Use newParent.append(this) to change parent
		/// </summary>
		public GUIElement Parent => m_parentElement;

		/// <summary>
		/// Gets the child elements of this element
		/// This is read only, use Append or Remove to add/remove child elements
		/// </summary>
		public IReadOnlyCollection<GUIElement> ChildElements => m_childElements;

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
				// Check width and height
				if (value.Width < 1) throw new ArgumentOutOfRangeException(nameof(value.Width), value.Width, "Cannot be less then 1");
				if (value.Height < 1) throw new ArgumentOutOfRangeException(nameof(value.Height), value.Height, "Cannot be less then 1");

				// Do a rectangle comparison to check if anything has changed, if so assign and set every controller boolean to true
				if (value != m_bounds) {
					m_bounds = value;
					m_hasBoundsChanged = true;
					m_hasSizeChanged = true;
					m_hasLocationChanged = true;
				}
			}
		}

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
		/// </summary>
		public bool Enabled {
			get => m_enabled;
			set {
				if (m_enabled != value) {
					// Even if new value is false, a redraw toggle is needed to "not draw" the element onto its parent
					ToggleRedraw();
				}

				m_enabled = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating if the element should be rendered. This might.. not work as expected, since it might still get mouse input
		/// </summary>
		public bool Visible {
			get => m_visible;
			set {
				if (m_visible != value) {
					// Even if new value is false, a redraw toggle is needed to "not draw" the element onto its parent
					ToggleRedraw();
				}

				m_visible = value;
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
				if (value < 1) throw new ArgumentOutOfRangeException(nameof(Width), value, "Cannot be less then 1");

				if (!MathUtil.NearEqual(value, m_bounds.Width)) {
					m_hasBoundsChanged = true;
					m_hasSizeChanged = true;
					m_bounds.Width = value;
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
				if (value < 1) throw new ArgumentOutOfRangeException(nameof(Height), value, "Cannot be less then 1");
				if (!MathUtil.NearEqual(value, m_bounds.Height)) {
					m_bounds.Height = value;
					m_hasBoundsChanged = true;
					m_hasSizeChanged = true;
					OnBoundsChangedDirect();
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
		/// Gets or sets the text of the element
		/// </summary>
		public string Text {
			get => m_text;
			set {
				if (m_text != value) {
					m_text = value;
					m_hasTextChanged = true;
				}
			}
		}

		/// <summary>
		/// Gets or sets the paragraph alignment of the element text
		/// </summary>
		public ParagraphAlignment ParagraphAlignment {
			get => m_guiText.ParagraphAlignment;
			set => m_guiText.ParagraphAlignment = value;
		}

		/// <summary>
		/// Gets or sets the text alignment of the element text
		/// </summary>
		public TextAlignment TextAlignment {
			get => m_guiText.TextAlignment;
			set => m_guiText.TextAlignment = value;
		}

		/// <summary>
		/// Gets or sets the word wrapping of the element text
		/// </summary>
		public WordWrapping WordWrapping {
			get => m_guiText.WordWrapping;
			set => m_guiText.WordWrapping = value;
		}

		/// <summary>
		/// Gets or sets the font used of the element text
		/// </summary>
		public string Font {
			get => m_guiText.Font;
			set => m_guiText.Font = value;
		}

		/// <summary>
		/// Gets or sets the font size of the element text
		/// </summary>
		public int FontSize {
			get => m_guiText.FontSize;
			set => m_guiText.FontSize = value;
		}

		public TextLayout TextLayout => m_guiText.GetCachedTextLayout();
		public TextFormat TextFormat => m_guiText.GetCachedTextFormat();

		/// <summary>
		/// Defaults to return this.ScreenBounds, but can be adjusted as desired
		/// </summary>
		protected virtual RectangleF MouseScreenBounds => ScreenBounds;

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
		/// Gets a value indicating if this control contains the mouse
		/// </summary>
		public bool ContainsMouse => m_containsMouse;

		#endregion

		#region Events

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
		public event Action TextChanged;

		/// <summary>
		/// Raised when this or a child receives focus
		/// </summary>
		public event Action ContainFocusGained;

		/// <summary>
		/// Raised when this or a child looses focus
		/// </summary>
		public event Action ContainFocusLost;

		public event Action Updated;


		public delegate void ParentChangeHandler(GUIElement newParent, GUIElement oldParent);

		public delegate void AppendHandler(GUIElement parent, GUIElement child);

		public event ParentChangeHandler ParentChanged;
		public event AppendHandler ChildAppended;
		public event AppendHandler ChildRemoved;
		public event AppendHandler ParentSet;
		public event AppendHandler ParentUnset;

		protected virtual void OnBoundsChanged() => BoundsChanged?.Invoke();
		protected virtual void OnBoundsChangedDirect() => BoundsChangedDirect?.Invoke();
		protected virtual void OnLocationChanged() => LocationChanged?.Invoke();
		protected virtual void OnResize() => Resized?.Invoke();

		protected virtual void OnFocusLost() => FocusLost?.Invoke();
		protected virtual void OnFocusGained() => FocusGained?.Invoke();

		protected virtual void OnMouseEnter() => MouseEnter?.Invoke();
		protected virtual void OnMouseLeave() => MouseLeave?.Invoke();
		protected virtual void OnMouseHover() => MouseHover?.Invoke();

		protected virtual void OnDragStart() => DragStart?.Invoke();
		protected virtual void OnDrag() => Drag?.Invoke();
		protected virtual void OnDragStop() => DragStop?.Invoke();

		protected virtual void OnMouseDown(GUIMouseEventArgs args) => MouseDown?.Invoke(args);
		protected virtual void OnMouseUp(GUIMouseEventArgs args) => MouseUp?.Invoke(args);
		protected virtual void OnMousePressed(GUIMouseEventArgs args) => MousePressed?.Invoke(args);
		protected virtual void OnClick(GUIMouseEventArgs args) => Click?.Invoke(args);
		protected virtual void OnDoubleClick(GUIMouseEventArgs args) => DoubleClick?.Invoke(args);

		protected virtual void OnKeyDown(List<Key> keys) => KeyDown?.Invoke(keys);
		protected virtual void OnKeyUp(List<Key> keys) => KeyUp?.Invoke(keys);
		protected virtual void OnKeyPressed(List<Key> keys) => KeyPressed?.Invoke(keys);
		protected virtual void OnTextInput(string text) => TextInput?.Invoke(text);
		protected virtual void OnRepeatKey(Key key) => RepeatKey?.Invoke(key);

		protected virtual void OnTextChanged() => TextChanged?.Invoke();

		protected virtual void OnContainFocusGained() => ContainFocusGained?.Invoke();
		protected virtual void OnContainFocusLost() => ContainFocusLost?.Invoke();

		protected virtual void OnParentChanged(GUIElement newParent, GUIElement oldParent) => ParentChanged?.Invoke(newParent, oldParent);
		protected virtual void OnChildAppended(GUIElement parent, GUIElement child) => ChildAppended?.Invoke(parent, child);
		protected virtual void OnChildRemoved(GUIElement parent, GUIElement child) => ChildRemoved?.Invoke(parent, child);
		protected virtual void OnParentSet(GUIElement parent, GUIElement child) => ParentSet?.Invoke(parent, child);
		protected virtual void OnParentUnset(GUIElement parent, GUIElement child) => ParentUnset?.Invoke(parent, child);

		#endregion

		#region Hierarchy

		/// <summary>
		/// Appends a element to this element
		/// </summary>
		/// <param name="element">The element ot append</param>
		/// <typeparam name="T">Element type</typeparam>
		/// <returns>The appended element</returns>
		public T Append<T>(T element) where T : GUIElement {
			// Store current parent for event invocation
			var previousParent = element.m_parentElement;

			// Remove from current parent
			element.m_parentElement?.Remove(element);

			// Add to child elements
			if (m_childElements.Contains(element) == false) {
				m_childElements.Add(element);
				// Invoke events
				OnChildAppended(this, element);
			}

			// Set parent
			element.m_parentElement = this;

			// Invoke events on child, but only if parent actually changed
			if (element.m_parentElement != previousParent) {
				element.OnParentChanged(this, previousParent);

				// Invoke event on child. Run after element.m_parentElement = this; so Parent property is correctly set
				element.OnParentSet(this, element);
			}

			return element;
		}

		/// <summary>
		/// Removes an element from this element
		/// </summary>
		/// <param name="element">The element to remove</param>
		public void Remove(GUIElement element) {
			if (m_childElements.Contains(element)) {
				// Remove from child elements
				m_childElements.Remove(element);

				// Run events, should only run when the collection has actually changed
				OnChildRemoved(this, element);
				// Run events on child
				element.OnParentUnset(this, element);
			}

			// Set parent to null
			element.m_parentElement = null;
		}


		/// <summary>
		/// Moves this element to the front of parents child elements
		/// </summary>
		public void MoveToFront() {
			m_parentElement?.MoveMeToFront(this);
		}

		/// <summary>
		/// Moves this element to the back of parents child elements
		/// </summary>
		public void MoveToBack() {
			m_parentElement?.MoveMeToBack(this);
		}

		/// <summary>
		/// Enqueues a move to front operation.
		/// </summary>
		private void MoveMeToFront(GUIElement target) {
			// No need to move if child count is just 1, and check that the target actually is a child
			if (m_childElements.Count > 1 && m_childElements.Contains(target)) {
				// This operation needs to be cached and run next frame, reason being that its usually called from within a update loop
				m_moveActions.Enqueue(() => {
					// Make a copy just to be safe
					var targetCopy = target;
					// Get current index
					var index = m_childElements.IndexOf(targetCopy);
					// Pop current index
					m_childElements.RemoveAt(index);
					// Add back to element gets in the last position
					m_childElements.Add(targetCopy);
				});
			}
		}

		/// <summary>
		/// Enqueues a move to back operation
		/// </summary>
		private void MoveMeToBack(GUIElement target) {
			// No need to move if child count is just 1, and check that the target actually is a child
			if (m_childElements.Count > 1 && m_childElements.Contains(target)) {
				// This operation needs to be cached and run next frame, reason being that its usually called from within a update loop
				m_moveActions.Enqueue(() => {
					// Make a copy just to be safe
					var targetCopy = target;
					// Get current index
					var index = m_childElements.IndexOf(targetCopy);
					// Pop current index
					m_childElements.RemoveAt(index);
					// Insert at index 0
					m_childElements.Insert(0, targetCopy);
				});
			}
		}

		/// <summary>
		/// Gets every child recursively
		/// </summary>
		public GUIElement[] AllChildren() {
			var result = new List<GUIElement>();
			AllChildrenRecursive(ref result);
			return result.ToArray();
		}

		/// <summary>
		/// Recurse function to get all elements in a hierarchy
		/// </summary>
		/// <param name="elements"></param>
		private void AllChildrenRecursive(ref List<GUIElement> elements) {
			elements.Add(this);
			foreach (var childElement in m_childElements) {
				childElement.AllChildrenRecursive(ref elements);
			}
		}

		/// <summary>
		/// Converts a screen position to elements local position
		/// </summary>
		/// <param name="screenPosition"></param>
		/// <returns></returns>
		public Vector2 ScreenToLocal(Vector2 screenPosition) {
			return screenPosition - ScreenBounds.Location;
		}

		/// <summary>
		/// Converts a local element position to screen position
		/// </summary>
		/// <param name="localPosition"></param>
		/// <returns></returns>
		public Vector2 LocalToScreen(Vector2 localPosition) {
			return localPosition + ScreenBounds.Location;
		}

		#endregion


		#region Update

		/// <summary>
		/// Pre updates to clear variables, move elements around, etc
		/// </summary>
		internal void PreFrame(GUISystem guiSystem) {
			if (m_enabled == false) return;

			// Reset all per frame variables
			MouseHovering = guiSystem.DragTarget == this;
			IsDragged = guiSystem.DragTarget == this;
			m_isFocused = guiSystem.FocusTarget == this;
			// m_isFocused = false;
			m_isMousePressed = false;
			m_containsMouse = false;

			if (m_isNewFocusTarget) {
				SetFocusTarget(this, guiSystem);
				m_isNewFocusTarget = false;
			}


			// Reshuffle children based on move actions added last frame
			if (m_moveActions.Count > 0) {
				foreach (var childElement in m_childElements) {
					childElement.ToggleRedraw();
				}
			}

			while (m_moveActions.Count > 0) {
				m_moveActions.Dequeue().Invoke();
			}

			// Get children to reset all variables
			foreach (var childElement in m_childElements) {
				childElement.PreFrame(guiSystem);
			}

			// Run pre update after sorting etc is complete
			OnPreUpdate();
		}

		/// <summary>
		/// Runs virtual on update on all children
		/// </summary>
		internal void Update() {
			if (m_enabled == false) return;

			for (int i = m_childElements.Count - 1; i >= 0; i--) {
				m_childElements[i].Update();
			}

			OnUpdate();
		}

		/// <summary>
		/// Late update, run before rendering
		/// </summary>
		internal void LateUpdate() {
			if (m_enabled == false) return;

			if (m_hasBoundsChanged) {
				// Flip trigger
				m_hasBoundsChanged = false;
				// Call events
				OnBoundsChanged();
				// Toggle a redraw if bounds has changed
				ToggleRedraw();
			}

			if (m_hasLocationChanged) {
				// Flip trigger
				m_hasLocationChanged = false;
				// Call events
				OnLocationChanged();
			}

			if (m_hasSizeChanged) {
				// Flip trigger
				m_hasSizeChanged = false;
				// Call events
				OnResize();
				// Toggle a texture resize
				ToggleResize();
			}

			if (m_hasTextChanged) {
				// Flip trigger
				m_hasTextChanged = false;
				// Toggle a redraw
				ToggleRedraw();
				// Call event
				OnTextChanged();
			}

			// Start with parent late update
			OnLateUpdate();

			// Then move to children
			for (int i = 0; i < m_childElements.Count; i++) {
				m_childElements[i].LateUpdate();
			}
		}


		/// <summary>
		/// Handles mouse input, returns a value indicating if the control has grabbed the mouse input
		/// </summary>
		internal bool HandleMouse(GUIMouseEventArgs args, GUISystem guiSystem) {
			// Get out of here if element is not enabled
			if (m_enabled == false) return false;
			// Might not want to handle mouse events if the element is not visible
			if (m_visible == false) return false;

			bool isMouseHandled = false;
			//bool containsMouse = MouseScreenBounds.Contains(args.MousePosition);
			m_containsMouse = MouseScreenBounds.Contains(args.MousePosition);

			// If drag target is currently set but left mouse button is not pressed, release the drag target
			if (guiSystem.DragTarget != null && args.LeftMousePressed == false) {
				guiSystem.DragTarget.OnDragStop();
				guiSystem.DragTarget = null;

				// TODO - might have to run left mouse up here if contained
			}

			// If drag target is this element, end the left mouse is being pressed, continue calling drag
			if (guiSystem.DragTarget == this && args.LeftMousePressed) {
				OnDrag();
				// TODO - should probably run MousePressed events here
				// Reason is that if we start pressing a button and move the mouse away from the button it should still look "pressed" while the mouse is down
				// Click event should only be called if mouse is up while its actually hovering over the element
			}

			// Should only propagate mouse checking, if this control actually contains the mouse position
			if (m_containsMouse) {
				// Running in reverse because the last added child should be the first to receive update check
				for (int i = m_childElements.Count - 1; i >= 0; i--) {
					var childElement = m_childElements[i];
					// Get information if child has handled mouse input
					isMouseHandled = childElement.HandleMouse(args, guiSystem);
					// If child has handled mouse input, break loop since we dont need to check any more children
					if (isMouseHandled) {
						break;
					}
				}
			}

			// Somethings getting dragged, but its not this element, we should not handle any input
			if (guiSystem.DragTarget != null && guiSystem.DragTarget != this) {
				return false;
			}

			// If mouse is not yet handled, and current element contains mouse, set hover target
			if (!isMouseHandled && m_containsMouse) {
				// TODO - This will set hover target to utmost child first, before setting it to the correct one if "CanReceiveMouseInput" on the child is false
				// TODO - It works, but is that a good idea?
				// TODO - This causes underlying elements to call Mouse Enter and Mouse Leave each frame since hover target switches to child element, then switches back to parent element even if it should never switch to the child element
				// TODO - Special rule if m_parentElement is null?
				// Set hover target makes sure that element can receive input before calling events atleast
				// Top level element should get hover even if it has "CanReceiveMouseInput" as false


				// If element can receive focus or parent element is null, set hover target (This is to allow base element to take hover, which in turn will allow mouse to "stop" hovering over any elements at all)
				if (CanReceiveMouseInput || m_parentElement == null) {
					SetHoverTarget(this, guiSystem);
				}
			}

			// If mouse has not yet been handled by a child element, try to handle it here
			if (!isMouseHandled && CanReceiveMouseInput) {
				// If element contains mouse, set hover to true
				if (m_containsMouse) {
					// Set variable to indicate that mouse is hovering to true
					MouseHovering = true;
					// Call events
					OnMouseHover();
					// Handled to true
					isMouseHandled = true;
					// Do input checking, like clicking etc
					HandleMouseInput(args, guiSystem);
				}
			}

			// Return a status indicating if this element has handled mouse input (Usually true for the outer most child that contains the mouse position)
			return isMouseHandled;
		}

		private bool m_isMousePressed;

		public bool IsMousePressed => m_isMousePressed;


		private void HandleMouseInput(GUIMouseEventArgs args, GUISystem guiSystem) {
			if (args.LeftMouseDown && guiSystem.DragTarget != this && Draggable) {
				guiSystem.DragTarget = this;
				OnDragStart();
			}

			if (args.LeftMousePressed || args.RightMousePressed) {
				OnMousePressed(args);
				m_isMousePressed = true;
			}

			if (args.LeftMouseDown || args.RightMouseDown) {
				OnMouseDown(args);
			}

			if (args.LeftMouseUp || args.RightMouseUp) {
				OnMouseUp(args);
				OnClick(args);
			}

			if (args.LeftDoubleClick || args.RightDoubleClick) {
				OnDoubleClick(args);
			}
		}


		internal bool HandleKeyboard(GUIKeyboardArgs args, GUISystem guiSystem) {
			// Get out of here if we cant receive any keyboard input
			if (CanReceiveKeyboardInput == false) {
				return false;
			}

			// Temp var to check if any keys were pressed when checking for input
			var result = false;

			// Make sure this is the focus target
			if (guiSystem.FocusTarget == this) {
				// Handle keyboard input based on args
				if (args.KeysUp.Count > 0) {
					OnKeyUp(args.KeysUp);
					result = true;
				}

				if (args.KeysDown.Count > 0) {
					OnKeyDown(args.KeysDown);
					result = true;
				}

				if (args.KeysPressed.Count > 0) {
					OnKeyPressed(args.KeysPressed);
					result = true;
				}

				if (string.IsNullOrEmpty(args.TextInput) == false) {
					OnTextInput(args.TextInput);
					result = true;
				}

				if (args.RepeatKey != null) {
					OnRepeatKey((Key) args.RepeatKey);
					result = true;
				}
			}

			// Return true if this control can capture input and result is true
			return CanCaptureKeyboardInput && result;
		}


		private void SetHoverTarget(GUIElement target, GUISystem guiSystem) {
			// If new target is different, run events
			if (guiSystem.HoverTarget != target) {
				// Check if current target is set, and that it can receive mouse input
				if (guiSystem.HoverTarget != null && guiSystem.HoverTarget.CanReceiveMouseInput) {
					// Call on leave
					guiSystem.HoverTarget.OnMouseLeave();
				}

				// Check if new target can receive input
				if (target.CanReceiveMouseInput) {
					// Call on enter
					target.OnMouseEnter();
				}

				// Apply new hover target. This should not be affected by CanReceiveMouseInput, since we want to set hover target to a base element if mouse is not over anything
				guiSystem.HoverTarget = target;
			}
		}

		public void Focus() {
			// Changed to this since focus changing needs gui system
			m_isNewFocusTarget = true;
		}

		private bool m_isNewFocusTarget;

		private void SetFocusTarget(GUIElement target, GUISystem guiSystem) {
			// If input target is null, all focus should be lost
			if (target == null) {
				if (guiSystem.FocusTarget != null) {
					// Need to reset all of focus target's parents that contain focus
					var parent = guiSystem.FocusTarget.Parent;
					// Recurse up through the tree to find all parents of the current focus target
					while (parent != null) {
						// If parent knows it contains focus, run disable contains focus and run events
						if (parent.m_containsFocus) {
							parent.m_containsFocus = false;
							parent.OnContainFocusLost();
						}

						parent = parent.Parent;
					}

					guiSystem.FocusTarget.m_isFocused = false;
					guiSystem.FocusTarget.OnFocusLost();
					guiSystem.FocusTarget = null;
				}
			} else {
				// Only switch if new target can receive focus
				if (target.CanReceiveFocus) {
					// If target exists, lose focus
					guiSystem.FocusTarget?.OnFocusLost();
					var currentFocusTarget = guiSystem.FocusTarget;
					guiSystem.FocusTarget = target;
					guiSystem.FocusTarget.OnFocusGained();
					CalculateContainsFocus(target, currentFocusTarget);
				}
			}
		}

		private static void CalculateContainsFocus(GUIElement newTarget, GUIElement oldTarget) {
			foreach (var el in newTarget.Lineage()) {
				if (el.m_containsFocus == false) {
					el.OnContainFocusGained();
				}

				el.m_containsFocus = true;
			}

			if (oldTarget != null) {
				foreach (var el in oldTarget.Lineage()) {
					var stillContains = false;
					foreach (var ch in el.AllChildren()) {
						if (ch == newTarget) {
							stillContains = true;
						}
					}

					if (el == newTarget) {
						stillContains = true;
					}

					if (!stillContains) {
						el.m_containsFocus = false;
						el.OnContainFocusLost();
					} else {
						// Can break if we found a element in the lineage that still should contain focus
						break;
					}
				}
			}
		}

		public IEnumerable<GUIElement> Lineage(bool includeSelf = true) {
			var target = this;
			if (includeSelf == false) {
				target = Parent;
			}

			while (target != null) {
				yield return target;
				target = target.Parent;
			}
		}

		#endregion

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
				m_renderTexture.RenderTarget.Transform = Matrix3x2.Translation(m_renderOffset);

				// Begin drawing
				m_renderTexture.RenderTarget.BeginDraw();

				// Clear rendertexture
				m_renderTexture.RenderTarget.Clear(Color.Transparent);

				// Render bounds has the same width and height as local bounds but are not offset on the x and y axis
				var renderBounds = m_bounds;
				// TODO - does this make sense?
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
				OnRedraw?.Invoke();

				// Render all children
				foreach (var childElement in m_childElements) {
					// Render child to this elements render texture
					childElement.RenderBitmap(m_renderTexture.RenderTarget, guiSystem);
				}

				if (UseClippingBounds) {
					m_renderTexture.RenderTarget.PopAxisAlignedClip();
				}

				// End drawing
				m_renderTexture.RenderTarget.EndDraw();

				// Toggle redraw
				m_redraw = false;
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
				// Then render our complete texture to callers render target
				renderTarget.DrawBitmap(m_renderTexture.Bitmap, m_bounds, 1.0F, BitmapInterpolationMode.Linear);
			}
		}


		public void ToggleRedraw() {
			// Should not need to toggle redraw if its already been toggled
			if (m_redraw) return; // TODO - Figure out if this check breaks anything
			// Set redraw to true
			m_redraw = true;
			// If this element needs a redraw, all parents also need to redraw.
			m_parentElement?.ToggleRedraw();
		}

		public void ToggleResize() {
			m_resizeRenderTexture = true;
		}

		#endregion

		#region Virtuals

		protected virtual void OnPreUpdate() { }
		protected virtual void OnUpdate() => Updated?.Invoke();
		protected virtual void OnLateUpdate() { }
		protected virtual void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout) { }
		protected virtual void OnDispose() { }

		#endregion

		#region Dispose

		public void Dispose() {
			// Remove parent (we dont call "Remove" here, since it changes the parents child element list
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
					m_childElements[0]?.Dispose();
					m_childElements.RemoveAt(0);
				}
			}

			OnDispose();
		}

		#endregion

		#region Helper Functions

		/// <summary>
		/// Moves the control by the input amount
		/// </summary>
		/// <param name="amount">Amount as a Vector2</param>
		public void Move(Vector2 amount) {
			this.X += amount.X;
			this.Y += amount.Y;
		}

		/// <summary>
		/// Moves the control by the input amount
		/// </summary>
		/// <param name="x">Amount of pixels to move in the X direction</param>
		/// <param name="y">Amount of pixels to move in the Y direction</param>
		public void Move(float x, float y) {
			this.X += x;
			this.Y += y;
		}

		#endregion
	}
}