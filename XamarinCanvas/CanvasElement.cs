using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using MonoDevelop.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace XamarinCanvas
{

	public enum ElementState {
		Prelight = 1,
		Pressed = 1 << 1,
	}

	public class LayoutEventArgs : EventArgs
	{
		public Cairo.Context Context { get; private set; }
		
		public LayoutEventArgs (Cairo.Context context)
		{
			Context = context;
		}
	}

	public class RenderEventArgs : EventArgs
	{
		public Cairo.Context Context { get; private set; }

		public RenderEventArgs (Cairo.Context context)
		{
			Context = context;
		}
	}

	public class MenuItemActivatedArgs : EventArgs
	{
		public MenuEntry Entry { get; private set; }

		public MenuItemActivatedArgs (MenuEntry entry)
		{
			Entry = entry;
		}

	}

	public class ButtonEventArgs : EventArgs
	{
		public double X { get; private set; }
		public double Y { get; private set; }
		public double XRoot { get; private set; }
		public double YRoot { get; private set; }
		public uint Button { get; private set; }
		public Gdk.ModifierType State { get; private set; }

		public ButtonEventArgs (double x, double y, double rootX, double rootY, uint button, Gdk.ModifierType state) 
		{
			X = x;
			Y = y;
			XRoot = rootX;
			YRoot = rootY;
			Button = button;
			State = state;
		}
	}

	public struct MenuEntry
	{
		public string Name;
		public object Data;

		public MenuEntry (string name, object data) 
		{
			Name = name;
			Data = data;
		}
	}

	public interface IContinuation<T>
	{
		void ContinueWith (Action<T> action);
	}

	public class Continuation<T> : IContinuation<T>
	{
		List<Action<T>> actions;

		public Continuation ()
		{
			actions = new List<Action<T>> ();
		}

		public void ContinueWith (Action<T> action)
		{
			actions.Add (action);
		}

		public void Invoke (T val)
		{
			foreach (var action in actions)
				action (val);
		}
	}

	public class CanvasElement : IComparable<CanvasElement>, Animatable
	{
		public virtual Gdk.CursorType Cursor { get {
				return Gdk.CursorType.XCursor;
			}
		}

		public bool NoChainOpacity { get; set; }
		public bool InputTransparent { get; set; }
		public bool Sensative { get; set; }
		public bool Draggable { get; set; }
		public bool CanFocus { get; set; }
		public bool HasFocus { get; private set; }
		public List<MenuEntry> MenuItems { get; set; }

		ElementState state;
		public ElementState State {
			get {
				return state;
			}
			private set {
				state = value;
				QueueDraw ();
			}
		}

		double x;
		public double X {
			get { return x; }
			set {
				x = value;
				transformValid = false;
			}
		}

		double y;
		public double Y {
			get {
				return y;
			}
			set {
				y = value;
				transformValid = false;
			}
		}

		double anchorX;
		public double AnchorX {
			get {
				return anchorX;
			}
			set {
				anchorX = value;
				transformValid = false;
			}
		}

		double anchorY;
		public double AnchorY {
			get {
				return anchorY;
			}
			set {
				anchorY = value;
				transformValid = false;
			}
		}

		public double Width { get; private set; }
		public double Height { get; private set; }

		double rotation;
		public double Rotation {
			get {
				return rotation;
			}
			set {
				rotation = value;
				transformValid = false;
			}
		}

		double scale;
		public double Scale {
			get {
				return scale;
			}
			set {
				scale = value;
				transformValid = false;
			}
		}

		public double Depth { get; set; }

		double opacity;
		public double Opacity {
			get {
				double result = opacity;
				if (Parent != null && !NoChainOpacity)
					result *= Parent.Opacity;

				return result;
			}
			set {
				opacity = Math.Max (0, Math.Min (1, value));
			}
		}

		/// <summary>
		/// User set width override for an element
		/// </summary>
		public double WidthRequest { get; set; }

		/// <summary>
		/// User set height override for an element
		/// </summary>
		public double HeightRequest { get; set; }

		/// <summary>
		/// Elements prefered allocation accounting for user set width request
		/// </summary>
		double preferedWidth;
		public double PreferedWidth {
			get {
				return WidthRequest == -1 ? preferedWidth : WidthRequest;
			}
			private set {
				preferedWidth = value;
			}
		}

		/// <summary>
		/// Elements prefered allocationsaccounting for user set height request
		/// </summary>
		double preferedHeight;
		public double PreferedHeight {
			get {
				return HeightRequest == -1 ? preferedHeight : HeightRequest;
			}
			private set {
				preferedHeight = value;
			}
		}

		public event EventHandler ClickEvent;
		public event EventHandler CanvasSet;

		public event EventHandler<RenderEventArgs> RenderEvent;
		public event EventHandler<LayoutEventArgs> LayoutEvent;

		public event EventHandler<MenuItemActivatedArgs> MenuItemActivatedEvent;

		public event EventHandler<ButtonEventArgs> ButtonPressEvent;
		public event EventHandler<ButtonEventArgs> ButtonReleaseEvent;

		public event EventHandler SizeChanged;
		public event EventHandler PreferedSizeChanged;

		bool transformValid;
		Cairo.Matrix transform;
		public Cairo.Matrix Transform { 
			get {
				if (!transformValid) {
					transform.InitIdentity ();
					transform.Translate (X + AnchorX, Y + AnchorY);
					transform.Rotate (Rotation);
					transform.Scale (Scale, Scale);
					transform.Translate (-AnchorX, -AnchorY);
					transformValid = true;
				}

				return transform;
			}
		}

		Cairo.Matrix inverse;
		public Cairo.Matrix InverseTransform { get {
				inverse.InitIdentity ();
				inverse.Multiply (Transform);

				var parent = Parent;
				while (parent != null) {
					inverse.Multiply (parent.Transform);
					parent = parent.Parent;
				}

				inverse.Invert ();
				return inverse;
			}
		}

		public virtual ReadOnlyCollection<CanvasElement> Children {
			get {
				return null;
			}
		}

		public CanvasElement Parent { get; set; }

		Canvas canvas;
		public Canvas Canvas {
			get {
				if (canvas == null && Parent != null)
					return Parent.Canvas;
				return canvas;
			}
			set {
				if (canvas == value)
					return;
				canvas = value;
				if (CanvasSet != null)
					CanvasSet (this, EventArgs.Empty);
			}
		}

		public CanvasElement ()
		{
			opacity = 1;
			Scale = 1;
			WidthRequest = HeightRequest = -1;
			Width = Height = -1;
			CanFocus = true;
			Sensative = true;
			inverse = new Cairo.Matrix ();
			transform = new Cairo.Matrix ();
		}

		public void LayoutOutline (Cairo.Context context)
		{
			OnLayoutOutline (context);
			if (LayoutEvent != null)
				LayoutEvent (this, new LayoutEventArgs (context));
		}

		public void Render (Cairo.Context context)
		{
			OnRender (context);
			if (RenderEvent != null)
				RenderEvent (this, new RenderEventArgs (context));
		}

		public void MouseIn ()
		{
			if (Canvas != null && Cursor != Gdk.CursorType.XCursor) {
				Canvas.GdkWindow.Cursor = new Gdk.Cursor (Cursor);
			}
			State |= ElementState.Prelight;
			OnMouseIn ();
		}

		public void MouseOut ()
		{
			if (Canvas != null && Cursor != Gdk.CursorType.XCursor) {
				Canvas.GdkWindow.Cursor = null;
			}
			State &= ~ElementState.Prelight;
			OnMouseOut ();
		}

		public void MouseMotion (double x, double y, Gdk.ModifierType state)
		{
			OnMouseMotion (x, y, state);
		}

		public void ButtonPress (ButtonEventArgs args) 
		{
			State |= ElementState.Pressed;
			OnButtonPress (args);

			if (ButtonPressEvent != null) {
				ButtonPressEvent (this, args);
			}
		}

		public void ButtonRelease (ButtonEventArgs args) 
		{
			State &= ~ElementState.Pressed;
			OnButtonRelease (args);

			if (ButtonReleaseEvent != null) {
				ButtonReleaseEvent (this, args);
			}
		}

		public void GrabBroken ()
		{
			State &= ~ElementState.Pressed;
		}

		public void FocusIn ()
		{
			HasFocus = true;
			OnFocusIn ();
		}

		public void FocusOut ()
		{
			HasFocus = false;
			OnFocusOut ();
		}

		public void Clicked (double x, double y, Gdk.ModifierType state)
		{
			OnClicked (x, y, state);
			if (ClickEvent != null)
				ClickEvent (this, EventArgs.Empty);
		}

		protected virtual void MoveChildFocus (bool reverse)
		{
			if (Children == null)
				return;

			var children = Children.AsEnumerable ();
			if (reverse) {
				children = children.Reverse ();
			}
			CanvasElement next = children.Where (c => c.CanFocus).SkipWhile (c => !c.HasFocus).Skip (1).FirstOrDefault ();
			if (next != null) {
				if (Canvas != null)
					Canvas.FocusElement (next);
			} else {
				if (Parent != null)
					Parent.MoveChildFocus (reverse);
			}
		}

		public void KeyPress (Gdk.EventKey evnt)
		{
			switch (evnt.Key) {
			case Gdk.Key.Tab:
				if (Parent != null)
					MoveChildFocus (false);
				break;
			case Gdk.Key.ISO_Left_Tab:
				if (Parent != null)
					MoveChildFocus (true);
				break;
			default:
				OnKeyPress (evnt);
				break;
			}
		}

		public void KeyRelease (Gdk.EventKey evnt)
		{
			OnKeyRelease (evnt);
		}

		private Cairo.PointD GetPoint(double t, Cairo.PointD p0, Cairo.PointD p1, Cairo.PointD p2, Cairo.PointD p3)
		{
			double cx = 3 * (p1.X - p0.X);
			double cy = 3 * (p1.Y - p0.Y);
			
			double bx = 3 * (p2.X - p1.X) - cx;
			double by = 3 * (p2.Y - p1.Y) - cy;
			
			double ax = p3.X - p0.X - cx - bx;
			double ay = p3.Y - p0.Y - cy - by;
			
			double Cube = t * t * t;
			double Square = t * t;
			
			double resX = (ax * Cube) + (bx * Square) + (cx * t) + p0.X;
			double resY = (ay * Cube) + (by * Square) + (cy * t) + p0.Y;
			
			return new Cairo.PointD(resX, resY);
		}

		public IContinuation<bool> CurveTo (double x1, double y1, double x2, double y2, double x3, double y3, uint length = 250, Func<float, float> easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;

			Continuation<bool> result = new Continuation<bool> ();

			Cairo.PointD start = new Cairo.PointD (X, Y);
			Cairo.PointD p1 = new Cairo.PointD (x1, y1);
			Cairo.PointD p2 = new Cairo.PointD (x2, y2);
			Cairo.PointD end = new Cairo.PointD (x3, y3);
			new Animation (f => {
				var position = GetPoint (f, start, p1, p2, end);
				X = position.X;
				Y = position.Y;
			}, 0, 1, easing)
				.Commit (this, "MoveTo", 16, length, finished: (f, a) => {
					result.Invoke (a);
				});

			return result;
		}

		public IContinuation<bool> RelMoveTo (double dx, double dy, uint length = 250, Func<float, float> easing = null)
		{
			return MoveTo (X + dx, Y + dy, length, easing);
		}

		public IContinuation<bool> RelRotateTo (double drotation, uint length = 250, Func<float, float> easing = null)
		{
			return RotateTo (Rotation + drotation, length, easing);
		}

		public IContinuation<bool> RelScaleTo (double dscale, uint length = 250, Func<float, float> easing = null)
		{
			return ScaleTo (Scale + dscale, length, easing);
		}

		public IContinuation<bool> MoveTo (double x, double y, uint length = 250, Func<float, float> easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;

			Continuation<bool> result = new Continuation<bool> ();

			new Animation ()
				.Insert (0, 1, new Animation (f => X = f, (float)X, (float)x, easing))
				.Insert (0, 1, new Animation (f => Y = f, (float)Y, (float)y, easing))
				.Commit (this, "MoveTo", 16, length, finished: (f, a) => {
					result.Invoke (a);
				});

			return result;
		}

		public IContinuation<bool> RotateTo (double roatation, uint length = 250, Func<float, float> easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;

			Continuation<bool> result = new Continuation<bool> ();

			new Animation (f => Rotation = f, (float)Rotation, (float)roatation, easing)
				.Commit (this, "RotateTo", 16, length, finished: (f, a) => {
					result.Invoke (a);
				});

			return result;
		}

		public IContinuation<bool> ScaleTo (double scale, uint length = 250, Func<float, float> easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;

			Continuation<bool> result = new Continuation<bool> ();

			new Animation (f => Scale = f, (float)Scale, (float)scale, easing)
				.Commit (this, "ScaleTo", 16, length, finished: (f, a) => {
					result.Invoke (a);
				});

			return result;
		}

		public IContinuation<bool> SizeTo (double width, double height, uint length = 250, Func<float, float> easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;

			Continuation<bool> result = new Continuation<bool> ();

			var wInterp = AnimationExtensions.Interpolate ((float)Width, (float)width);
			var hInterp = AnimationExtensions.Interpolate ((float)Height, (float)height);
			new Animation ()
				.Insert (0, 1, new Animation (f => SetSize (wInterp (f), hInterp (f)) , 0, 1, easing))
				.Commit (this, "SizeTo", 16, length, finished: (f, a) => {
					result.Invoke (a);
				});

			return result;
		}

		public IContinuation<bool> FadeTo (double opacity, uint length = 250, Func<float, float> easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;

			Continuation<bool> result = new Continuation<bool> ();
			new Animation (f => Opacity = f, (float)Opacity, (float)opacity, easing)
				.Commit (this, "FadeTo", 16, length, finished: (f, a) => {
					result.Invoke (a);
				});

			return result;
		}

		public void CancelAnimations ()
		{
			this.AbortAnimation ("MoveTo");
			this.AbortAnimation ("RotateTo");
			this.AbortAnimation ("ScaleTo");
		}

		public void SetSize (double width, double height)
		{
			Width = width;
			Height = height;

			SizeAllocated (Width, Height);
			if (SizeChanged != null)
				SizeChanged (this, EventArgs.Empty);
			QueueDraw ();
		}

		protected void SetPreferedSize (double width, double height)
		{
			PreferedWidth = width;
			PreferedHeight = height;

			if (PreferedSizeChanged != null)
				PreferedSizeChanged (this, EventArgs.Empty);
		}

		void SizeAllocated (double width, double height)
		{
			OnSizeAllocated (width, height);
		}

		protected virtual void OnMouseIn () {}
		protected virtual void OnMouseOut () {}
		protected virtual void OnMouseMotion (double x, double y, Gdk.ModifierType state) {}
		protected virtual void OnButtonPress (ButtonEventArgs args) {}
		protected virtual void OnButtonRelease (ButtonEventArgs args) {}
		protected virtual void OnClicked (double x, double y, Gdk.ModifierType state) {}
		protected virtual void OnFocusIn () {}
		protected virtual void OnFocusOut () {}
		protected virtual void OnKeyPress (Gdk.EventKey evnt) {}
		protected virtual void OnKeyRelease (Gdk.EventKey evnt) {}
		protected virtual void OnSizeRequested (ref double width, ref double height) {}
		protected virtual void OnSizeAllocated (double width, double height) {}

		protected virtual void OnLayoutOutline (Cairo.Context context) {}

		protected virtual void OnRender (Cairo.Context context) 
		{	
			if (Children == null)
				return;
			foreach (var child in Children) {
				context.Save ();
				child.Canvas = Canvas;
				context.Transform (child.Transform);
				child.Render (context);
				context.Restore ();
			}
		}

		public void QueueDraw ()
		{
			if (Canvas != null) {
				Canvas.ChildNeedDraw ();
			}
		}

		Gdk.Point menuPosition;
		public void ShowMenu (int rootX, int rootY, uint button)
		{
			if (MenuItems == null || !MenuItems.Any ())
				return;
			Gtk.Menu menu = new Gtk.Menu ();

			foreach (var item in MenuItems) {
				Gtk.MenuItem menuItem = new Gtk.MenuItem (item.Name);
				var tmp = item;
				menuItem.Activated += (sender, e) => {
					if (MenuItemActivatedEvent != null)
						MenuItemActivatedEvent (this, new MenuItemActivatedArgs (tmp));
				};
				menu.Append (menuItem);
				menuItem.Show ();
			}

			
			menuPosition = new Gdk.Point (rootX, rootY);
			menu.Popup (null, null, PositionMenu, button, Gtk.Global.CurrentEventTime);
		}

		void PositionMenu (Gtk.Menu menu, out int x, out int y, out bool pushIn)
		{
			x = menuPosition.X;
			y = menuPosition.Y;
			pushIn = false;

		}

		#region IComparable implementation

		public int CompareTo (CanvasElement other)
		{
			return Depth.CompareTo (other.Depth);
		}

		#endregion
	}
	
}
