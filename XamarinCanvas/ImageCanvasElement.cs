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
	public class ImageCanvasElement : CanvasElement
	{
		Gdk.Pixbuf pbuf;
		SurfaceWrapper surfaceCache;

		public ImageCanvasElement (Gdk.Pixbuf pbuf)
		{
			SetImage (pbuf);
		}

		public void SetImage (Gdk.Pixbuf pbuf)
		{
			this.pbuf = pbuf;
			SetPreferedSize (pbuf.Width, pbuf.Height);
			if (surfaceCache != null)
				surfaceCache.Dispose ();
			surfaceCache = null;
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			context.Rectangle (0, 0, Width, Height);
		}

		protected override void OnRender (Cairo.Context context)
		{
			if (surfaceCache == null) {
				// will improve with CGLayer surfaces
				surfaceCache = new SurfaceWrapper (context, pbuf);
			}
			context.SetSourceSurface (surfaceCache.Surface, 0, 0);
			double opacity = Opacity;
			if (opacity == 1)
				context.Paint ();
			else
				context.PaintWithAlpha (Opacity);

			base.OnRender (context);
		}
	}
	
}
