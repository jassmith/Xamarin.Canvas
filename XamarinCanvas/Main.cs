using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gtk;

using MonoDevelop.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace XamarinCanvas
{
	public class GroupCanvasElement : CanvasElement
	{
		List<CanvasElement> children;

		public override IEnumerable<CanvasElement> Children {
			get {
				return children.AsEnumerable ();
			}
		}

		public GroupCanvasElement ()
		{
			InputTransparent = true;
			children = new List<CanvasElement> ();
		}

		public void Add (CanvasElement element)
		{
			children.Add (element);
			element.Parent = this;
			QueueDraw ();
		}

		public void Remove (CanvasElement element)
		{
			if (children.Remove (element)) {
				element.Parent = null;
				QueueDraw ();
			}
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{

		}

		protected override void OnRender (Cairo.Context context)
		{

		}
	}

	public class BoxCanvasElement : CanvasElement
	{
		Cairo.Color color;

		public BoxCanvasElement (Cairo.Color color)
		{
			this.color = color;
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			context.Rectangle (-100, -100, 200, 200);
		}

		protected override void OnRender (Cairo.Context context)
		{
			OnLayoutOutline (context);
			context.Color = color;
			context.Fill ();
		}

		protected override void OnMouseIn ()
		{
		}

		protected override void OnMouseOut ()
		{
		}

		protected override void OnMouseMotion (int x, int y, Gdk.ModifierType state)
		{

		}

		protected override void OnFocusIn ()
		{

		}

		protected override void OnFocusOut ()
		{

		}

		protected override void OnClicked (int x, int y, Gdk.ModifierType state)
		{
			Random r = new Random ();
			
			RotateTo (r.Next (-10, 10) * r.NextDouble (), 1000, Easing.SpringOut);
//			CurveTo (r.Next (0, Canvas.Allocation.Width), r.Next (0, Canvas.Allocation.Height), 
//			         r.Next (0, Canvas.Allocation.Width), r.Next (0, Canvas.Allocation.Height), 
//			         r.Next (0, Canvas.Allocation.Width), r.Next (0, Canvas.Allocation.Height), 
//			         1000, Easing.CubicInOut);
//			ScaleTo ((0.1 + r.NextDouble ()) * 1.4, 1000, Easing.BounceOut);
		}
	}

	class MainClass
	{
		public static void Main (string[] args)
		{
			Application.Init ();
			MainWindow win = new MainWindow ();
			win.Resize (800, 800);

			Random r1 = new Random ();
			Canvas canvas = new Canvas ();
			GroupCanvasElement group = new GroupCanvasElement ();

			CanvasElement element = new BoxCanvasElement (new Cairo.Color (r1.NextDouble (), r1.NextDouble (), r1.NextDouble ()));
			element.X = -120;
			element.Y = 120;
			element.Draggable = true;
			group.Add (element);

			element = new BoxCanvasElement (new Cairo.Color (r1.NextDouble (), r1.NextDouble (), r1.NextDouble ()));
			element.X = -120;
			element.Y = -120;
			element.Draggable = true;
			group.Add (element);

			element = new BoxCanvasElement (new Cairo.Color (r1.NextDouble (), r1.NextDouble (), r1.NextDouble ()));
			element.X = 120;
			element.Y = 120;
			element.Draggable = true;
			group.Add (element);

			element = new BoxCanvasElement (new Cairo.Color (r1.NextDouble (), r1.NextDouble (), r1.NextDouble ()));
			element.X = 120;
			element.Y = -120;
			element.Draggable = true;
			group.Add (element);

			canvas.AddElement (group);
			group.Rotation = Math.PI / 4;
			group.X = 400;
			group.Y = 400;

			win.Add (canvas);

			win.ShowAll ();
			Application.Run ();
		}
	}
}
