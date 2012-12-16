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
	public class LabelCanvasElement : CanvasElement
	{
		public double XAlign { get; set; }
		public double YAlign { get; set; }

		public bool ClipInputToTextExtents { get; set; }

		Cairo.Color Color { get; set; }

		Gdk.Size pixelSize;

		string text;
		public string Text {
			get {
				return text;
			}
			set {
				text = value;
				markup = null;
				UpdateSize ();
				QueueDraw ();
			}
		}

		string markup;
		public string Markup {
			get {
				return markup;
			}
			set {
				markup = value;
				text = null;
				UpdateSize ();
				QueueDraw ();
			}
		}

		public LabelCanvasElement (string label)
		{
			Text = label;
			Color = new Cairo.Color (0, 0, 0);

			CanvasSet += (sender, e) => UpdateSize ();
		}

		protected override void OnLayoutOutline (Cairo.Context context)
		{
			if (ClipInputToTextExtents) {
				Pango.Layout layout = GetLayout ();
				layout.Width = Pango.Units.FromPixels ((int)Width);
				
				int w, h;
				layout.GetPixelSize (out w, out h);
				
				int x = (int)Width - w;
				x = (int) (x * XAlign);
				
				int y = (int)Height - h;
				y = (int) (y * YAlign);
				
				context.Rectangle (x, y, w, h);
			} else {
				context.Rectangle (0, 0, Width, Height);
			}

		}

		void UpdateSize ()
		{
			if (Canvas == null)
				return;

			if (cachedLayout != null) {
				cachedLayout.Dispose ();
				cachedLayout = null;
			}

			int w, h;
			GetLayout ().GetPixelSize (out w, out h);
			SetPreferedSize (w, h);

			pixelSize = new Gdk.Size (w, h);
		}

		protected override void OnRender (Cairo.Context context)
		{
			Pango.Layout layout = GetLayout ();
			layout.Width = Pango.Units.FromPixels ((int)Width);

			int w = pixelSize.Width;
			int h = pixelSize.Height;

			int x = (int)Width - w;
			x = (int) (x * XAlign);

			int y = (int)Height - h;
			y = (int) (y * YAlign);

			context.MoveTo (x, y);
			context.Color = Color.MultiplyAlpha (Opacity);
			Pango.CairoHelper.ShowLayout (context, layout);

			base.OnRender (context);
		}

		Pango.Layout cachedLayout;
		Pango.Layout GetLayout ()
		{
			if (cachedLayout == null) {
				cachedLayout = new Pango.Layout (Canvas.PangoContext);
				if (Markup != null) {
					cachedLayout.SetMarkup (Markup);
				} else if (Text != null) {
					cachedLayout.SetText (Text);
				}
			}
			return cachedLayout;
		}
	}
	
}
