using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using MonoDevelop.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace XamarinCanvas
{

	public class Canvas : Gtk.EventBox, Animatable
	{
		GroupCanvasElement rootElement;

		MouseTracker tracker;

		CanvasElement hoveredElement;
		CanvasElement HoveredElement {
			get {
				return hoveredElement;
			}
			set {
				if (value == hoveredElement)
					return;

				if (MouseGrabElement != null)
					return;

				var old = hoveredElement;
				hoveredElement = value;

				if (old != null)
					old.MouseOut ();


				if (hoveredElement != null)
					hoveredElement.MouseIn ();
			}
		}

		Gdk.Point DragOffset { get; set; }
		Gdk.Point DragStart { get; set; }
		bool dragging;

		CanvasElement mouseGrabElement;
		CanvasElement MouseGrabElement {
			get {
				return mouseGrabElement;
			}
			set {
				mouseGrabElement = value;
				HoveredElement = value;
			}
		}

		CanvasElement LastFocusedElement { get; set; }

		CanvasElement focusedElement;
		CanvasElement FocusedElement {
			get {
				return focusedElement;
			}
			set {
				if (value == focusedElement)
					return;

				var old = focusedElement;
				focusedElement = value;

				if (old != null)
					old.FocusOut ();

				if (focusedElement != null)
					focusedElement.FocusIn ();
			}
		}

		public Canvas ()
		{
			AppPaintable = true;
			CanFocus = true;
			rootElement = new GroupCanvasElement ();
			rootElement.Canvas = this;

			AddEvents ((int)(Gdk.EventMask.AllEventsMask));
			tracker = new MouseTracker (this);
		}

		public void AddElement (CanvasElement element)
		{
			rootElement.Add (element);
			QueueDraw ();
		}

		public void RemoveElement (CanvasElement element)
		{
			rootElement.Remove (element);
			QueueDraw ();
		}

		public void ChildNeedDraw ()
		{
			QueueDraw ();
			if (tracker.Hovered)
				HoveredElement = GetInputElementAt (tracker.MousePosition.X, tracker.MousePosition.Y);
		}

		CanvasElement GetInputElementAt (Cairo.Context context, CanvasElement root, double x, double y)
		{
			foreach (var element in root.Children.Reverse ()) {
				var result = GetInputElementAt (context, element, x, y);
				if (result != null)
					return result;
			}

			if (root.InputTransparent)
				return null;

			context.Save ();
			root.LayoutOutline (context);
			
			double dx = x;
			double dy = y;
			root.InverseTransform.TransformPoint (ref dx, ref dy);
			
			if (context.InFill (dx, dy)) {
				context.NewPath ();
				context.Restore ();
				return root;
			}
			context.NewPath ();
			context.Restore ();
			return null;
		}

		CanvasElement GetInputElementAt (double x, double y)
		{
			using (var context = Gdk.CairoHelper.Create (GdkWindow)) {
				var result = GetInputElementAt (context, rootElement, x, y);
				if (result == null)
					return result;
				return result.Sensative ? result : null;
			}
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			double dx = evnt.X;
			double dy = evnt.Y;
			if (MouseGrabElement != null) {
				if (MouseGrabElement.Draggable && evnt.State.HasFlag (Gdk.ModifierType.Button1Mask)) {
					if (!dragging && (Math.Abs (DragStart.X - dx) > 5 || Math.Abs (DragStart.Y - dy) > 5))
						dragging = true;

					if (dragging) {
						MouseGrabElement.Parent.InverseTransform.TransformPoint (ref dx, ref dy);
						MouseGrabElement.X = dx - DragOffset.X;
						MouseGrabElement.Y = dy - DragOffset.Y;
						QueueDraw ();
					}
				} else {
					var point = TransformPoint (MouseGrabElement, evnt.X, evnt.Y);
					MouseGrabElement.MouseMotion (point.X, point.Y, evnt.State);
				}
			} else {
				HoveredElement = GetInputElementAt (evnt.X, evnt.Y);
				if (HoveredElement != null) {
					var point = TransformPoint (HoveredElement, evnt.X, evnt.Y);
					HoveredElement.MouseMotion (point.X, point.Y, evnt.State);
				}
			}

			return base.OnMotionNotifyEvent (evnt);
		}

		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			return base.OnEnterNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			HoveredElement = null;
			return base.OnLeaveNotifyEvent (evnt);
		}

		Cairo.PointD TransformPoint (CanvasElement element, double x, double y)
		{
			element.InverseTransform.TransformPoint (ref x, ref y);
			return new Cairo.PointD (x, y);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			int x = (int)evnt.X;
			int y = (int)evnt.Y;
			var element = GetInputElementAt (x, y); 

			HasFocus = true;

			MouseGrabElement = element;
			if (MouseGrabElement != null) {
				MouseGrabElement.CancelAnimations ();
				DragStart = new Gdk.Point (x, y);

				double dx = x;
				double dy = y;
				MouseGrabElement.Parent.InverseTransform.TransformPoint (ref dx, ref dy);
				DragOffset = new Gdk.Point ((int) (dx - MouseGrabElement.X), (int) (dy - MouseGrabElement.Y));

				var transformedPoint = TransformPoint (MouseGrabElement, x, y);
				MouseGrabElement.ButtonPress (transformedPoint.X, transformedPoint.Y, evnt.Button, evnt.State);
			}

			return true;
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			int x = (int)evnt.X;
			int y = (int)evnt.Y;

			if (MouseGrabElement != null) {
				var element = GetInputElementAt (x, y);
				var point = TransformPoint (MouseGrabElement, x, y);
				MouseGrabElement.ButtonRelease (point.X, point.Y, evnt.Button, evnt.State);

				if (element == MouseGrabElement && !dragging) {
					element.Clicked (point.X, point.Y, evnt.State);
					if (element.CanFocus)
						FocusedElement = element;
				}
			}

			dragging = false;
			MouseGrabElement = null;
			return true;
		}

		protected override bool OnFocusInEvent (Gdk.EventFocus evnt)
		{
			if (LastFocusedElement != null)
				FocusedElement = LastFocusedElement;
			return base.OnFocusInEvent (evnt);
		}

		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			if (FocusedElement != null)
				LastFocusedElement = FocusedElement;
			FocusedElement = null;
			return base.OnFocusOutEvent (evnt);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (FocusedElement != null)
				FocusedElement.KeyPress (evnt);
			return base.OnKeyPressEvent (evnt);
		}

		protected override bool OnKeyReleaseEvent (Gdk.EventKey evnt)
		{
			if (FocusedElement != null)
				FocusedElement.KeyRelease (evnt);
			return base.OnKeyReleaseEvent (evnt);
		}

		void RenderElement (Cairo.Context context, CanvasElement element)
		{
			element.Canvas = this;

			context.Save ();
			context.Transform (element.Transform);
			element.Render (context);
			context.Restore ();

			context.Save ();
			element.PrepChildRender (context);
			if (element.Children != null)
				foreach (var child in element.Children)
					RenderElement (context, child);
			context.Restore ();

		}
		
		void Paint (Cairo.Context context)
		{
			context.Operator = Cairo.Operator.Source;
			context.Color = new Cairo.Color (0, 0, 0, 0);
			context.Paint ();
			context.Operator = Cairo.Operator.Over;

			RenderElement (context, rootElement);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (GdkWindow)) {
				Paint (context);
			}
			return true;
		}
	}
}

