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

		public ImageCanvasElement (Gdk.Pixbuf pbuf)
		{
			SetImage (pbuf);
		}

		public void SetImage (Gdk.Pixbuf pbuf)
		{
			this.pbuf = pbuf;
			SetSize (pbuf.Width, pbuf.Height);
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			context.Rectangle (0, 0, Width, Height);
		}

		protected override void OnRender (Cairo.Context context)
		{
			Gdk.CairoHelper.SetSourcePixbuf (context, pbuf, 0, 0);
			context.Paint ();
		}
	}
	
}
