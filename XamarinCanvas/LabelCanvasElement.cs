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
				using (Pango.Layout layout = GetLayout ()) {
					layout.Width = Pango.Units.FromPixels ((int)Width);
					
					int w, h;
					layout.GetPixelSize (out w, out h);
					
					int x = (int)Width - w;
					x = (int) (x * XAlign);
					
					int y = (int)Height - h;
					y = (int) (y * YAlign);
					
					context.Rectangle (x, y, w, h);
				}
			} else {
				context.Rectangle (0, 0, Width, Height);
			}

		}

		void UpdateSize ()
		{
			if (Canvas == null)
				return;

			int finalWidth, finalHeight;
			if (WidthRequest >= 0 && HeightRequest >= 0)
			{
				finalWidth = (int)WidthRequest;
				finalHeight = (int)HeightRequest;
			} else {
				using (Pango.Layout layout = GetLayout ()) {
					int w, h;
					layout.GetPixelSize (out w, out h);
					finalWidth = WidthRequest >= 0 ? (int)WidthRequest : w;
					finalHeight = HeightRequest >= 0 ? (int)HeightRequest : h;
				}
			}
			SetSize (finalWidth, finalHeight);
		}

		protected override void OnRender (Cairo.Context context)
		{
			using (Pango.Layout layout = GetLayout ()) {
				if (Width != -1)
					layout.Width = Pango.Units.FromPixels ((int)Width);

				int w, h;
				layout.GetPixelSize (out w, out h);

				int x = Width == -1 ? 0 : (int)Width - w;
				x = (int) (x * XAlign);

				int y = Width == -1 ? 0 : (int)Height - h;
				y = (int) (y * YAlign);

				context.MoveTo (x, y);
				context.Color = Color;
				Pango.CairoHelper.ShowLayout (context, layout);
			}
		}

		Pango.Layout GetLayout ()
		{
			Pango.Layout result = new Pango.Layout (Canvas.PangoContext);
			if (Markup != null) {
				result.SetMarkup (Markup);
			} else if (Text != null) {
				result.SetText (Text);
			}
			return result;
		}
	}
	
}
