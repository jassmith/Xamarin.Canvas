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
				HoveredElement = value;
				mouseGrabElement = value;
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
			VisibleWindow = false;
			CanFocus = true;
			rootElement = new GroupCanvasElement ();
			rootElement.Canvas = this;

			AddEvents ((int)(Gdk.EventMask.AllEventsMask));
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

		public void FocusElement (CanvasElement element)
		{
			if (element.CanFocus) {
				FocusedElement = element;
			}
		}

		CanvasElement GetInputElementAt (Cairo.Context context, CanvasElement root, double x, double y)
		{
			if (root.InputTransparent)
				return null;

			var children = root.Children;

			if (children != null) {
				// Manual loop to avoid excessive creation of iterators
				for (int i = children.Count - 1; i >= 0; i--) {
					var result = GetInputElementAt (context, children[i], x, y);
					if (result != null)
						return result;
				}
			}

			context.Save ();
			root.LayoutOutline (context);
			var point = TransformPoint (root, x, y);
			
			if (context.InFill (point.X, point.Y)) {
				context.NewPath ();
				context.Restore ();
				return root;
			}
			context.NewPath ();
			context.Restore ();
			return null;
		}

		CanvasElement GetInputElementAt (Cairo.Context context, double x, double y)
		{
			var result = GetInputElementAt (context, rootElement, x, y);
			if (result == null)
				return result;
			return result.Sensative ? result : null;
		}

		CanvasElement GetInputElementAt (double x, double y)
		{
			using (var context = Gdk.CairoHelper.Create (GdkWindow)) {
				return GetInputElementAt (context, x, y);
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
						var point = TransformPoint (MouseGrabElement.Parent, dx, dy);
						MouseGrabElement.X = point.X - DragOffset.X;
						MouseGrabElement.Y = point.Y - DragOffset.Y;
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
				var point = TransformPoint (MouseGrabElement.Parent, dx, dy);
				DragOffset = new Gdk.Point ((int) (point.X - MouseGrabElement.X), (int) (point.Y - MouseGrabElement.Y));

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
			return true;
		}

		protected override bool OnKeyReleaseEvent (Gdk.EventKey evnt)
		{
			if (FocusedElement != null)
				FocusedElement.KeyRelease (evnt);
			return true;
		}

		void RenderElement (Cairo.Context context, CanvasElement element)
		{
			element.Canvas = this;

			context.Transform (element.Transform);
			element.Render (context);
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			rootElement.SetSize (allocation.Width, allocation.Height);
			base.OnSizeAllocated (allocation);
		}
		
		void Paint (Cairo.Context context)
		{
			RenderElement (context, rootElement);
		}

		public void ChildNeedDraw ()
		{
			QueueDraw ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.Translate (Allocation.X, Allocation.Y);
				context.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				context.Clip ();
				Paint (context);
			}
			return true;
		}
	}
}

