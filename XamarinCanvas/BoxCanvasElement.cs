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

			SetPreferedSize (200, 200);
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			context.Rectangle (0, 0, Width, Height);
		}

		protected override void OnRender (Cairo.Context context)
		{
			OnLayoutOutline (context);
			context.Color = color.MultiplyAlpha (Opacity);
			context.Fill ();

			base.OnRender (context);
		}
	}
	
}
