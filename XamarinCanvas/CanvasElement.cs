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
		public Cairo.Context Context { get; set; }
		
		public LayoutEventArgs (Cairo.Context context)
		{
			Context = context;
		}
	}

	public class RenderEventArgs : EventArgs
	{
		public Cairo.Context Context { get; set; }

		public RenderEventArgs (Cairo.Context context)
		{
			Context = context;
		}
	}

	public class CanvasElement : IComparable<CanvasElement>, Animatable
	{
		public bool InputTransparent { get; set; }
		public bool Sensative { get; set; }
		public bool Draggable { get; set; }
		public bool CanFocus { get; set; }
		public bool HasFocus { get; private set; }

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
				if (Parent != null)
					result *= Parent.Opacity;

				return result;
			}
			set {
				opacity = value;
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

		public event EventHandler SizeChanged;
		public event EventHandler PreferedSizeChanged;

		bool transformValid;
		Cairo.Matrix transform;
		internal Cairo.Matrix Transform { 
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

		bool inverseValid;
		Cairo.Matrix inverse;
		internal Cairo.Matrix InverseTransform { get {
//				if (!inverseValid) {
					inverse.InitIdentity ();
					inverse.Multiply (Transform);
					
					var parent = Parent;
					while (parent != null) {
						inverse.Multiply (parent.Transform);
						parent = parent.Parent;
					}
					
					inverse.Invert ();
//				}
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
			State |= ElementState.Prelight;
			OnMouseIn ();
		}

		public void MouseOut ()
		{
			State &= ~ElementState.Prelight;
			OnMouseOut ();
		}

		public void MouseMotion (double x, double y, Gdk.ModifierType state)
		{
			OnMouseMotion (x, y, state);
		}

		public void ButtonPress (double x, double y, uint button, Gdk.ModifierType state) 
		{
			State |= ElementState.Pressed;
			OnButtonPress (x, y, button, state);
		}

		public void ButtonRelease (double x, double y, uint button, Gdk.ModifierType state) 
		{
			State &= ~ElementState.Pressed;
			OnButtonRelease (x, y, button, state);
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

		public void KeyPress (Gdk.EventKey evnt)
		{
			OnKeyPress (evnt);
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

		public void CurveTo (double x1, double y1, double x2, double y2, double x3, double y3, uint length = 250, Func<float, float> easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;
			Cairo.PointD start = new Cairo.PointD (X, Y);
			Cairo.PointD p1 = new Cairo.PointD (x1, y1);
			Cairo.PointD p2 = new Cairo.PointD (x2, y2);
			Cairo.PointD end = new Cairo.PointD (x3, y3);
			new Animation (f => {
				var position = GetPoint (f, start, p1, p2, end);
				X = position.X;
				Y = position.Y;
			}, 0, 1, easing)
				.Commit (this, "MoveTo", 16, length);
		}

		public void RelMoveTo (double dx, double dy, uint length = 250, Func<float, float> easing = null)
		{
			MoveTo (X + dx, Y + dy, length, easing);
		}

		public void RelRotateTo (double drotation, uint length = 250, Func<float, float> easing = null)
		{
			RotateTo (Rotation + drotation, length, easing);
		}

		public void RelScaleTo (double dscale, uint length = 250, Func<float, float> easing = null)
		{
			ScaleTo (Scale + dscale, length, easing);
		}

		public void MoveTo (double x, double y, uint length = 250, Func<float, float> easing = null)
		{
			if (x == X && y == Y)
				return;

			if (easing == null)
				easing = Easing.Linear;
			new Animation ()
				.Insert (0, 1, new Animation (f => X = f, (float)X, (float)x, easing))
				.Insert (0, 1, new Animation (f => Y = f, (float)Y, (float)y, easing))
				.Commit (this, "MoveTo", 16, length);
		}

		public void RotateTo (double roatation, uint length = 250, Func<float, float> easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;
			new Animation (f => Rotation = f, (float)Rotation, (float)roatation, easing)
				.Commit (this, "RotateTo", 16, length);
		}

		public void ScaleTo (double scale, uint length = 250, Func<float, float> easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;
			new Animation (f => Scale = f, (float)Scale, (float)scale, easing)
				.Commit (this, "ScaleTo", 16, length);
		}

		public void SizeTo (double width, double height, uint length = 250, Func<float, float> easing = null)
		{
			if (width == Width && height == Height)
				return;
			
			if (easing == null)
				easing = Easing.Linear;
			new Animation ()
				.Insert (0, 1, new Animation (f => Width = f, (float)Width, (float)width, easing))
				.Insert (0, 1, new Animation (f => Height = f, (float)Height, (float)height, easing))
				.Commit (this, "SizeTo", 16, length);
		}

		public void CancelAnimations ()
		{
			// Fixme : needs to abort all animations
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
		protected virtual void OnButtonPress (double x, double y, uint button, Gdk.ModifierType state) {}
		protected virtual void OnButtonRelease (double x, double y, uint button, Gdk.ModifierType state) {}
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

		#region IComparable implementation

		public int CompareTo (CanvasElement other)
		{
			return Depth.CompareTo (other.Depth);
		}

		#endregion
	}
	
}
