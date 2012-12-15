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

		public double X { get; set; }
		public double Y { get; set; }
		public double AnchorX { get; set; }
		public double AnchorY { get; set; }
		public double Width { get; private set; }
		public double Height { get; private set; }
		public double Rotation { get; set; }
		public double Scale { get; set; }
		public double Depth { get; set; }

		public double WidthRequest { get; set; }
		public double HeightRequest { get; set; }

		public event EventHandler ClickEvent;
		public event EventHandler CanvasSet;

		public event EventHandler<RenderEventArgs> RenderEvent;
		public event EventHandler<LayoutEventArgs> LayoutEvent;
		public event EventHandler<RenderEventArgs> PrepChildRenderEvent;

		public event EventHandler SizeChanged;

		internal Cairo.Matrix GroupTransform { get {
				var result = new Cairo.Matrix ();
				result.InitIdentity ();
				result.Translate (X, Y);
				result.Translate (AnchorX, AnchorY);
				result.Rotate (Rotation);
				result.Scale (Scale, Scale);
				result.Translate (-AnchorX, -AnchorY);

				if (Parent != null)
					result.Multiply (Parent.GroupTransform);

				return result;
			}
		}

		internal Cairo.Matrix Transform { get {
				var result = new Cairo.Matrix ();
				result.InitIdentity ();
				result.Translate (X, Y);
				result.Translate (AnchorX, AnchorY);
				result.Rotate (Rotation);
				result.Scale (Scale, Scale);
				result.Translate (-AnchorX, -AnchorY);
				
				if (Parent != null)
					result.Multiply (Parent.GroupTransform);
				
				return result;
			}
		}

		internal Cairo.Matrix InverseTransform { get {
				var result = Transform;
				result.Invert ();
				return result;
			}
		}

		public virtual IEnumerable<CanvasElement> Children {
			get {
				return Enumerable.Empty<CanvasElement> ();
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
			Scale = 1;
			WidthRequest = HeightRequest = -1;
			Width = Height = -1;
			CanFocus = true;
			Sensative = true;
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

		/// <summary>
		/// Rendering method called after a containers children have been rendered. Useful for unsetting clips/etc.
		/// </summary>
		public void PrepChildRender (Cairo.Context context)
		{
			OnPrepChildRender (context);
			if (PrepChildRenderEvent != null)
				PrepChildRenderEvent (this, new RenderEventArgs (context));
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

		public void MoveTo (double x, double y, uint length = 250, Func<float, float> easing = null)
		{
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
		protected virtual void OnSizeAllocated (double width, double height) {}

		protected virtual void OnLayoutOutline (Cairo.Context context) {}
		protected virtual void OnRender (Cairo.Context context) {}
		protected virtual void OnPrepChildRender (Cairo.Context context) {}

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
