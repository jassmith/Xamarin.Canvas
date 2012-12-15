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

	public class BoxCanvasElement : CanvasElement
	{
		Cairo.Color color;

		public BoxCanvasElement (Cairo.Color color, int width = 1, int height = 1)
		{
			this.color = color;
			AnchorX = width / 2.0;
			AnchorY = height / 2.0;

			SetSize (200, 200);
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			context.Rectangle (0, 0, Width, Height);
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

		protected override void OnMouseMotion (double x, double y, Gdk.ModifierType state)
		{
		}

		protected override void OnFocusIn ()
		{

		}

		protected override void OnFocusOut ()
		{

		}

		protected override void OnClicked (double x, double y, Gdk.ModifierType state)
		{
			Random r = new Random ();
			
			RotateTo (r.Next (-10, 10) * r.NextDouble (), 1000, Easing.SpringOut);
//			CurveTo (r.Next (0, Canvas.Allocation.Width), r.Next (0, Canvas.Allocation.Height), 
//			         r.Next (0, Canvas.Allocation.Width), r.Next (0, Canvas.Allocation.Height), 
//			         r.Next (0, Canvas.Allocation.Width), r.Next (0, Canvas.Allocation.Height), 
//			         1000, Easing.CubicInOut);
			ScaleTo ((0.1 + r.NextDouble ()) * 1.4, 1000, Easing.BounceOut);
		}
	}
	
}
